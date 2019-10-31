﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable 0162
#define CPR

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FASTER.core
{
    public unsafe partial class FasterKV<Key, Value, Input, Output, Context, Functions> : FasterBase, IFasterKV<Key, Value, Input, Output, Context>
        where Key : new()
        where Value : new()
        where Functions : IFunctions<Key, Value, Input, Output, Context>
    {
        enum LatchOperation : byte
        {
            None,
            ReleaseShared,
            ReleaseExclusive
        }

        #region Read Operation

        /// <summary>
        /// Read operation. Computes the 'output' from 'input' and current value corresponding to 'key'.
        /// When the read operation goes pending, once the record is retrieved from disk, InternalContinuePendingRead
        /// function is used to complete the operation.
        /// </summary>
        /// <param name="key">Key of the record.</param>
        /// <param name="input">Input required to compute output from value.</param>
        /// <param name="output">Location to store output computed from input and value.</param>
        /// <param name="userContext">User context for the operation, in case it goes pending.</param>
        /// <param name="pendingContext">Pending context used internally to store the context of the operation.</param>
        /// <returns>
        /// <list type="table">
        ///     <listheader>
        ///     <term>Value</term>
        ///     <term>Description</term>
        ///     </listheader>
        ///     <item>
        ///     <term>SUCCESS</term>
        ///     <term>The output has been computed using current value of 'key' and 'input'; and stored in 'output'.</term>
        ///     </item>
        ///     <item>
        ///     <term>RECORD_ON_DISK</term>
        ///     <term>The record corresponding to 'key' is on disk and the operation.</term>
        ///     </item>
        ///     <item>
        ///     <term>CPR_SHIFT_DETECTED</term>
        ///     <term>A shift in version has been detected. Synchronize immediately to avoid violating CPR consistency.</term>
        ///     </item>
        /// </list>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal OperationStatus InternalRead(
                                    ref Key key,
                                    ref Input input,
                                    ref Output output,
                                    ref Context userContext,
                                    ref PendingContext pendingContext)
        {
            var status = default(OperationStatus);
            var bucket = default(HashBucket*);
            var slot = default(int);
            var logicalAddress = Constants.kInvalidAddress;
            var physicalAddress = default(long);
            var latestRecordVersion = -1;

            var hash = comparer.GetHashCode64(ref key);
            var tag = (ushort)((ulong)hash >> Constants.kHashTagShift);

            if (threadCtx.Value.phase != Phase.REST)
                HeavyEnter(hash);

            #region Trace back for record in in-memory HybridLog
            HashBucketEntry entry = default(HashBucketEntry);
            var tagExists = FindTag(hash, tag, ref bucket, ref slot, ref entry);
            if (tagExists)
            {
                logicalAddress = entry.Address;

                if (UseReadCache && ReadFromCache(ref key, ref logicalAddress, ref physicalAddress, ref latestRecordVersion))
                {
                    if (threadCtx.Value.phase == Phase.PREPARE && latestRecordVersion != -1 && latestRecordVersion > threadCtx.Value.version)
                    {
                        status = OperationStatus.CPR_SHIFT_DETECTED;
                        goto CreatePendingContext; // Pivot thread
                    }
                    functions.SingleReader(ref key, ref input, ref readcache.GetValue(physicalAddress), ref output);
                    return OperationStatus.SUCCESS;
                }

                if (logicalAddress >= hlog.HeadAddress)
                {
                    physicalAddress = hlog.GetPhysicalAddress(logicalAddress);
                    if (latestRecordVersion == -1)
                        latestRecordVersion = hlog.GetInfo(physicalAddress).Version;

                    if (!comparer.Equals(ref key, ref hlog.GetKey(physicalAddress)))
                    {
                        logicalAddress = hlog.GetInfo(physicalAddress).PreviousAddress;
                        TraceBackForKeyMatch(ref key,
                                                logicalAddress,
                                                hlog.HeadAddress,
                                                out logicalAddress,
                                                out physicalAddress);
                    }
                }
            }
            else
            {
                // no tag found
                return OperationStatus.NOTFOUND;
            }
            #endregion

            if (threadCtx.Value.phase != Phase.REST)
            {
                switch (threadCtx.Value.phase)
                {
                    case Phase.PREPARE:
                        {
                            if (latestRecordVersion != -1 && latestRecordVersion > threadCtx.Value.version)
                            {
                                status = OperationStatus.CPR_SHIFT_DETECTED;
                                goto CreatePendingContext; // Pivot thread
                            }
                            break; // Normal processing
                        }
                    case Phase.GC:
                        {
                            GarbageCollectBuckets(hash);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }

            #region Normal processing

            // Mutable region (even fuzzy region is included here)
            if (logicalAddress >= hlog.SafeReadOnlyAddress)
            {
                if (hlog.GetInfo(physicalAddress).Tombstone)
                    return OperationStatus.NOTFOUND;

                functions.ConcurrentReader(ref key, ref input, ref hlog.GetValue(physicalAddress), ref output);
                return OperationStatus.SUCCESS;
            }

            // Immutable region
            else if (logicalAddress >= hlog.HeadAddress)
            {
                if (hlog.GetInfo(physicalAddress).Tombstone)
                    return OperationStatus.NOTFOUND;

                functions.SingleReader(ref key, ref input, ref hlog.GetValue(physicalAddress), ref output);
                return OperationStatus.SUCCESS;
            }

            // On-Disk Region
            else if (logicalAddress >= hlog.BeginAddress)
            {
                status = OperationStatus.RECORD_ON_DISK;

                if (threadCtx.Value.phase == Phase.PREPARE)
                {
                    if (!HashBucket.TryAcquireSharedLatch(bucket))
                    {
                        status = OperationStatus.CPR_SHIFT_DETECTED;
                    }
                }

                goto CreatePendingContext;
            }

            // No record found
            else
            {
                return OperationStatus.NOTFOUND;
            }

            #endregion

            #region Create pending context
            CreatePendingContext:
            {

                pendingContext.type = OperationType.READ;
                pendingContext.key = hlog.GetKeyContainer(ref key);
                pendingContext.input = input;
                pendingContext.output = output;
                pendingContext.userContext = userContext;
                pendingContext.entry.word = entry.word;
                pendingContext.logicalAddress = logicalAddress;
                pendingContext.version = threadCtx.Value.version;
                pendingContext.serialNum = threadCtx.Value.serialNum + 1;
            }
            #endregion

            return status;
        }

        /// <summary>
        /// Continue a pending read operation. Computes 'output' from 'input' and value corresponding to 'key'
        /// obtained from disk. Optionally, it copies the value to tail to serve future read/write requests quickly.
        /// </summary>
        /// <param name="ctx">The thread (or session) context to execute operation in.</param>
        /// <param name="request">Async response from disk.</param>
        /// <param name="pendingContext">Pending context corresponding to operation.</param>
        /// <returns>
        /// <list type = "table" >
        ///     <listheader>
        ///     <term>Value</term>
        ///     <term>Description</term>
        ///     </listheader>
        ///     <item>
        ///     <term>SUCCESS</term>
        ///     <term>The output has been computed and stored in 'output'.</term>
        ///     </item>
        /// </list>
        /// </returns>
        internal OperationStatus InternalContinuePendingRead(
                            FasterExecutionContext ctx,
                            AsyncIOContext<Key, Value> request,
                            ref PendingContext pendingContext)
        {
            Debug.Assert(pendingContext.version == ctx.version);

            if (request.logicalAddress >= hlog.BeginAddress)
            {
                Debug.Assert(hlog.GetInfoFromBytePointer(request.record.GetValidPointer()).Version <= ctx.version);

                if (hlog.GetInfoFromBytePointer(request.record.GetValidPointer()).Tombstone)
                    return OperationStatus.NOTFOUND;

                functions.SingleReader(ref pendingContext.key.Get(), ref pendingContext.input,
                                       ref hlog.GetContextRecordValue(ref request), ref pendingContext.output);

                if (CopyReadsToTail || UseReadCache)
                {
                    InternalContinuePendingReadCopyToTail(ctx, request, ref pendingContext);
                }
            }
            else
                return OperationStatus.NOTFOUND;

            return OperationStatus.SUCCESS;
        }

        /// <summary>
        /// Copies the record read from disk to tail of the HybridLog. 
        /// </summary>
        /// <param name="ctx"> The thread(or session) context to execute operation in.</param>
        /// <param name="request">Async response from disk.</param>
        /// <param name="pendingContext">Pending context corresponding to operation.</param>
        internal void InternalContinuePendingReadCopyToTail(
                                    FasterExecutionContext ctx,
                                    AsyncIOContext<Key, Value> request,
                                    ref PendingContext pendingContext)
        {
            Debug.Assert(pendingContext.version == ctx.version);

            var recordSize = default(int);
            var bucket = default(HashBucket*);
            var slot = default(int);
            var logicalAddress = Constants.kInvalidAddress;
            var physicalAddress = default(long);
            var latestRecordVersion = default(int);

            var hash = comparer.GetHashCode64(ref pendingContext.key.Get());
            var tag = (ushort)((ulong)hash >> Constants.kHashTagShift);

            #region Trace back record in in-memory HybridLog
            var entry = default(HashBucketEntry);
            FindOrCreateTag(hash, tag, ref bucket, ref slot, ref entry, hlog.BeginAddress);
            logicalAddress = entry.word & Constants.kAddressMask;

            if (UseReadCache)
                SkipReadCache(ref logicalAddress, ref latestRecordVersion);
            var latestLogicalAddress = logicalAddress;

            if (logicalAddress >= hlog.HeadAddress)
            {
                physicalAddress = hlog.GetPhysicalAddress(logicalAddress);
                if (!comparer.Equals(ref pendingContext.key.Get(), ref hlog.GetKey(physicalAddress)))
                {
                    logicalAddress = hlog.GetInfo(physicalAddress).PreviousAddress;
                    TraceBackForKeyMatch(ref pendingContext.key.Get(),
                                            logicalAddress,
                                            hlog.HeadAddress,
                                            out logicalAddress,
                                            out physicalAddress);
                }
            }
            #endregion

            if (logicalAddress > pendingContext.entry.Address)
            {
                // Give up early
                return;
            }

            #region Create new copy in mutable region
            physicalAddress = (long)request.record.GetValidPointer();
            recordSize = hlog.GetRecordSize(physicalAddress);

            long newLogicalAddress, newPhysicalAddress;
            if (UseReadCache)
            {
                BlockAllocateReadCache(recordSize, out newLogicalAddress);
                newPhysicalAddress = readcache.GetPhysicalAddress(newLogicalAddress);
                RecordInfo.WriteInfo(ref readcache.GetInfo(newPhysicalAddress), ctx.version,
                                    true, false, false,
                                    entry.Address);
                readcache.ShallowCopy(ref pendingContext.key.Get(), ref readcache.GetKey(newPhysicalAddress));
                functions.SingleWriter(ref pendingContext.key.Get(),
                                       ref hlog.GetContextRecordValue(ref request),
                                       ref readcache.GetValue(newPhysicalAddress));
            }
            else
            {
                BlockAllocate(recordSize, out newLogicalAddress);
                newPhysicalAddress = hlog.GetPhysicalAddress(newLogicalAddress);
                RecordInfo.WriteInfo(ref hlog.GetInfo(newPhysicalAddress), ctx.version,
                               true, false, false,
                               latestLogicalAddress);
                hlog.ShallowCopy(ref pendingContext.key.Get(), ref hlog.GetKey(newPhysicalAddress));
                functions.SingleWriter(ref pendingContext.key.Get(),
                                       ref hlog.GetContextRecordValue(ref request),
                                       ref hlog.GetValue(newPhysicalAddress));
            }


            var updatedEntry = default(HashBucketEntry);
            updatedEntry.Tag = tag;
            updatedEntry.Address = newLogicalAddress & Constants.kAddressMask;
            updatedEntry.Pending = entry.Pending;
            updatedEntry.Tentative = false;
            updatedEntry.ReadCache = UseReadCache;

            var foundEntry = default(HashBucketEntry);
            foundEntry.word = Interlocked.CompareExchange(
                                            ref bucket->bucket_entries[slot],
                                            updatedEntry.word,
                                            entry.word);
            if (foundEntry.word != entry.word)
            {
                if (!UseReadCache) hlog.GetInfo(newPhysicalAddress).Invalid = true;
                // We don't retry, just give up
            }
            #endregion
        }

        #endregion

        #region Upsert Operation

        /// <summary>
        /// Upsert operation. Replaces the value corresponding to 'key' with provided 'value', if one exists 
        /// else inserts a new record with 'key' and 'value'.
        /// </summary>
        /// <param name="key">key of the record.</param>
        /// <param name="value">value to be updated to (or inserted if key does not exist).</param>
        /// <param name="userContext">User context for the operation, in case it goes pending.</param>
        /// <param name="pendingContext">Pending context used internally to store the context of the operation.</param>
        /// <returns>
        /// <list type="table">
        ///     <listheader>
        ///     <term>Value</term>
        ///     <term>Description</term>
        ///     </listheader>
        ///     <item>
        ///     <term>SUCCESS</term>
        ///     <term>The value has been successfully replaced(or inserted)</term>
        ///     </item>
        ///     <item>
        ///     <term>RETRY_LATER</term>
        ///     <term>Cannot  be processed immediately due to system state. Add to pending list and retry later</term>
        ///     </item>
        ///     <item>
        ///     <term>CPR_SHIFT_DETECTED</term>
        ///     <term>A shift in version has been detected. Synchronize immediately to avoid violating CPR consistency.</term>
        ///     </item>
        /// </list>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal OperationStatus InternalUpsert(
                            ref Key key, ref Value value,
                            ref Context userContext,
                            ref PendingContext pendingContext)
        {
            var status = default(OperationStatus);
            var bucket = default(HashBucket*);
            var slot = default(int);
            var logicalAddress = Constants.kInvalidAddress;
            var physicalAddress = default(long);
            var latchOperation = default(LatchOperation);
            var version = default(int);
            var latestRecordVersion = -1;

            var hash = comparer.GetHashCode64(ref key);
            var tag = (ushort)((ulong)hash >> Constants.kHashTagShift);

            if (threadCtx.Value.phase != Phase.REST)
                HeavyEnter(hash);

            #region Trace back for record in in-memory HybridLog
            var entry = default(HashBucketEntry);
            FindOrCreateTag(hash, tag, ref bucket, ref slot, ref entry, hlog.BeginAddress);
            logicalAddress = entry.Address;

            if (UseReadCache)
                SkipAndInvalidateReadCache(ref logicalAddress, ref latestRecordVersion, ref key);
            var latestLogicalAddress = logicalAddress;

            if (logicalAddress >= hlog.ReadOnlyAddress)
            {
                physicalAddress = hlog.GetPhysicalAddress(logicalAddress);
                if (latestRecordVersion == -1)
                    latestRecordVersion = hlog.GetInfo(physicalAddress).Version;
                if (!comparer.Equals(ref key, ref hlog.GetKey(physicalAddress)))
                {
                    logicalAddress = hlog.GetInfo(physicalAddress).PreviousAddress;
                    TraceBackForKeyMatch(ref key,
                                        logicalAddress,
                                        hlog.ReadOnlyAddress,
                                        out logicalAddress,
                                        out physicalAddress);
                }
            }
            #endregion

            // Optimization for most common case
            if (threadCtx.Value.phase == Phase.REST && logicalAddress >= hlog.ReadOnlyAddress && !hlog.GetInfo(physicalAddress).Tombstone)
            {
                if (functions.ConcurrentWriter(ref key, ref value, ref hlog.GetValue(physicalAddress)))
                {
                    return OperationStatus.SUCCESS;
                }
            }

            #region Entry latch operation
            if (threadCtx.Value.phase != Phase.REST)
            {
                switch (threadCtx.Value.phase)
                {
                    case Phase.PREPARE:
                        {
                            version = threadCtx.Value.version;
                            if (HashBucket.TryAcquireSharedLatch(bucket))
                            {
                                // Set to release shared latch (default)
                                latchOperation = LatchOperation.ReleaseShared;
                                if (latestRecordVersion != -1 && latestRecordVersion > version)
                                {
                                    status = OperationStatus.CPR_SHIFT_DETECTED;
                                    goto CreatePendingContext; // Pivot Thread
                                }
                                break; // Normal Processing
                            }
                            else
                            {
                                status = OperationStatus.CPR_SHIFT_DETECTED;
                                goto CreatePendingContext; // Pivot Thread
                            }
                        }
                    case Phase.IN_PROGRESS:
                        {
                            version = (threadCtx.Value.version - 1);
                            if (latestRecordVersion != -1 && latestRecordVersion <= version)
                            {
                                if (HashBucket.TryAcquireExclusiveLatch(bucket))
                                {
                                    // Set to release exclusive latch (default)
                                    latchOperation = LatchOperation.ReleaseExclusive;
                                    goto CreateNewRecord; // Create a (v+1) record
                                }
                                else
                                {
                                    status = OperationStatus.RETRY_LATER;
                                    goto CreatePendingContext; // Go Pending
                                }
                            }
                            break; // Normal Processing
                        }
                    case Phase.WAIT_PENDING:
                        {
                            version = (threadCtx.Value.version - 1);
                            if (latestRecordVersion != -1 && latestRecordVersion <= version)
                            {
                                if (HashBucket.NoSharedLatches(bucket))
                                {
                                    goto CreateNewRecord; // Create a (v+1) record
                                }
                                else
                                {
                                    status = OperationStatus.RETRY_LATER;
                                    goto CreatePendingContext; // Go Pending
                                }
                            }
                            break; // Normal Processing
                        }
                    case Phase.WAIT_FLUSH:
                        {
                            version = (threadCtx.Value.version - 1);
                            if (latestRecordVersion != -1 && latestRecordVersion <= version)
                            {
                                goto CreateNewRecord; // Create a (v+1) record
                            }
                            break; // Normal Processing
                        }
                    default:
                        break;
                }
            }
            #endregion

            Debug.Assert(latestRecordVersion <= threadCtx.Value.version);

            #region Normal processing

            // Mutable Region: Update the record in-place
            if (logicalAddress >= hlog.ReadOnlyAddress && !hlog.GetInfo(physicalAddress).Tombstone)
            {
                if (functions.ConcurrentWriter(ref key, ref value, ref hlog.GetValue(physicalAddress)))
                {
                    status = OperationStatus.SUCCESS;
                    goto LatchRelease; // Release shared latch (if acquired)
                }
            }

            // All other regions: Create a record in the mutable region
            #endregion

            #region Create new record in the mutable region
            CreateNewRecord:
            {
                // Immutable region or new record
                var recordSize = hlog.GetRecordSize(ref key, ref value);
                BlockAllocate(recordSize, out long newLogicalAddress);
                var newPhysicalAddress = hlog.GetPhysicalAddress(newLogicalAddress);
                RecordInfo.WriteInfo(ref hlog.GetInfo(newPhysicalAddress),
                               threadCtx.Value.version,
                               true, false, false,
                               latestLogicalAddress);
                hlog.ShallowCopy(ref key, ref hlog.GetKey(newPhysicalAddress));
                functions.SingleWriter(ref key, ref value,
                                       ref hlog.GetValue(newPhysicalAddress));

                var updatedEntry = default(HashBucketEntry);
                updatedEntry.Tag = tag;
                updatedEntry.Address = newLogicalAddress & Constants.kAddressMask;
                updatedEntry.Pending = entry.Pending;
                updatedEntry.Tentative = false;

                var foundEntry = default(HashBucketEntry);
                foundEntry.word = Interlocked.CompareExchange(
                                        ref bucket->bucket_entries[slot],
                                        updatedEntry.word, entry.word);

                if (foundEntry.word == entry.word)
                {
                    status = OperationStatus.SUCCESS;
                    goto LatchRelease;
                }
                else
                {
                    hlog.GetInfo(newPhysicalAddress).Invalid = true;
                    status = OperationStatus.RETRY_NOW;
                    goto LatchRelease;
                }
            }
            #endregion

            #region Create pending context
            CreatePendingContext:
            {
                pendingContext.type = OperationType.UPSERT;
                pendingContext.key = hlog.GetKeyContainer(ref key);
                pendingContext.value = hlog.GetValueContainer(ref value);
                pendingContext.userContext = userContext;
                pendingContext.entry.word = entry.word;
                pendingContext.logicalAddress = logicalAddress;
                pendingContext.version = threadCtx.Value.version;
                pendingContext.serialNum = threadCtx.Value.serialNum + 1;
            }
            #endregion

            #region Latch release
            LatchRelease:
            {
                switch (latchOperation)
                {
                    case LatchOperation.ReleaseShared:
                        HashBucket.ReleaseSharedLatch(bucket);
                        break;
                    case LatchOperation.ReleaseExclusive:
                        HashBucket.ReleaseExclusiveLatch(bucket);
                        break;
                    default:
                        break;
                }
            }
            #endregion

            if (status == OperationStatus.RETRY_NOW)
            {
                return InternalUpsert(ref key, ref value, ref userContext, ref pendingContext);
            }
            else
            {
                return status;
            }
        }

        #endregion

        #region RMW Operation

        /// <summary>
        /// Read-Modify-Write Operation. Updates value of 'key' using 'input' and current value.
        /// Pending operations are processed either using InternalRetryPendingRMW or 
        /// InternalContinuePendingRMW.
        /// </summary>
        /// <param name="key">key of the record.</param>
        /// <param name="input">input used to update the value.</param>
        /// <param name="userContext">user context corresponding to operation used during completion callback.</param>
        /// <param name="pendingContext">pending context created when the operation goes pending.</param>
        /// <returns>
        /// <list type="table">
        ///     <listheader>
        ///     <term>Value</term>
        ///     <term>Description</term>
        ///     </listheader>
        ///     <item>
        ///     <term>SUCCESS</term>
        ///     <term>The value has been successfully updated(or inserted).</term>
        ///     </item>
        ///     <item>
        ///     <term>RECORD_ON_DISK</term>
        ///     <term>The record corresponding to 'key' is on disk. Issue async IO to retrieve record and retry later.</term>
        ///     </item>
        ///     <item>
        ///     <term>RETRY_LATER</term>
        ///     <term>Cannot  be processed immediately due to system state. Add to pending list and retry later.</term>
        ///     </item>
        ///     <item>
        ///     <term>CPR_SHIFT_DETECTED</term>
        ///     <term>A shift in version has been detected. Synchronize immediately to avoid violating CPR consistency.</term>
        ///     </item>
        /// </list>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal OperationStatus InternalRMW(
                                   ref Key key, ref Input input,
                                   ref Context userContext,
                                   ref PendingContext pendingContext)
        {
            var recordSize = default(int);
            var bucket = default(HashBucket*);
            var slot = default(int);
            var logicalAddress = Constants.kInvalidAddress;
            var physicalAddress = default(long);
            var version = default(int);
            var latestRecordVersion = -1;
            var status = default(OperationStatus);
            var latchOperation = LatchOperation.None;

            var hash = comparer.GetHashCode64(ref key);
            var tag = (ushort)((ulong)hash >> Constants.kHashTagShift);

            if (threadCtx.Value.phase != Phase.REST)
                HeavyEnter(hash);

            #region Trace back for record in in-memory HybridLog
            var entry = default(HashBucketEntry);
            FindOrCreateTag(hash, tag, ref bucket, ref slot, ref entry, hlog.BeginAddress);
            logicalAddress = entry.Address;

            // For simplicity, we don't let RMW operations use read cache
            if (UseReadCache)
                SkipReadCache(ref logicalAddress, ref latestRecordVersion);
            var latestLogicalAddress = logicalAddress;

            if (logicalAddress >= hlog.HeadAddress)
            {
                physicalAddress = hlog.GetPhysicalAddress(logicalAddress);
                latestRecordVersion = hlog.GetInfo(physicalAddress).Version;

                if (!comparer.Equals(ref key, ref hlog.GetKey(physicalAddress)))
                {
                    logicalAddress = hlog.GetInfo(physicalAddress).PreviousAddress;
                    TraceBackForKeyMatch(ref key, logicalAddress,
                                            hlog.HeadAddress,
                                            out logicalAddress,
                                            out physicalAddress);
                }
            }
            #endregion

            // Optimization for the most common case
            if (threadCtx.Value.phase == Phase.REST && logicalAddress >= hlog.ReadOnlyAddress && !hlog.GetInfo(physicalAddress).Tombstone)
            {
                if (functions.InPlaceUpdater(ref key, ref input, ref hlog.GetValue(physicalAddress)))
                {
                    return OperationStatus.SUCCESS;
                }
            }

            #region Entry latch operation
            if (threadCtx.Value.phase != Phase.REST)
            {
                switch (threadCtx.Value.phase)
                {
                    case Phase.PREPARE:
                        {
                            version = threadCtx.Value.version;
                            if (HashBucket.TryAcquireSharedLatch(bucket))
                            {
                                // Set to release shared latch (default)
                                latchOperation = LatchOperation.ReleaseShared;
                                if (latestRecordVersion != -1 && latestRecordVersion > version)
                                {
                                    status = OperationStatus.CPR_SHIFT_DETECTED;
                                    goto CreateFailureContext; // Pivot Thread
                                }
                                break; // Normal Processing
                            }
                            else
                            {
                                status = OperationStatus.CPR_SHIFT_DETECTED;
                                goto CreateFailureContext; // Pivot Thread
                            }
                        }
                    case Phase.IN_PROGRESS:
                        {
                            version = (threadCtx.Value.version - 1);
                            if (latestRecordVersion <= version)
                            {
                                if (HashBucket.TryAcquireExclusiveLatch(bucket))
                                {
                                    // Set to release exclusive latch (default)
                                    latchOperation = LatchOperation.ReleaseExclusive;
                                    goto CreateNewRecord; // Create a (v+1) record
                                }
                                else
                                {
                                    status = OperationStatus.RETRY_LATER;
                                    goto CreateFailureContext; // Go Pending
                                }
                            }
                            break; // Normal Processing
                        }
                    case Phase.WAIT_PENDING:
                        {
                            version = (threadCtx.Value.version - 1);
                            if (latestRecordVersion != -1 && latestRecordVersion <= version)
                            {
                                if (HashBucket.NoSharedLatches(bucket))
                                {
                                    goto CreateNewRecord; // Create a (v+1) record
                                }
                                else
                                {
                                    status = OperationStatus.RETRY_LATER;
                                    goto CreateFailureContext; // Go Pending
                                }
                            }
                            break; // Normal Processing
                        }
                    case Phase.WAIT_FLUSH:
                        {
                            version = (threadCtx.Value.version - 1);
                            if (latestRecordVersion != -1 && latestRecordVersion <= version)
                            {
                                goto CreateNewRecord; // Create a (v+1) record
                            }
                            break; // Normal Processing
                        }
                    default:
                        break;
                }
            }
            #endregion

            Debug.Assert(latestRecordVersion <= threadCtx.Value.version);

            #region Normal processing

            // Mutable Region: Update the record in-place
            if (logicalAddress >= hlog.ReadOnlyAddress && !hlog.GetInfo(physicalAddress).Tombstone)
            {
                if (FoldOverSnapshot)
                {
                    Debug.Assert(hlog.GetInfo(physicalAddress).Version == threadCtx.Value.version);
                }

                if (functions.InPlaceUpdater(ref key, ref input, ref hlog.GetValue(physicalAddress)))
                {
                    status = OperationStatus.SUCCESS;
                    goto LatchRelease; // Release shared latch (if acquired)
                }
            }

            // Fuzzy Region: Must go pending due to lost-update anomaly
            else if (logicalAddress >= hlog.SafeReadOnlyAddress && !hlog.GetInfo(physicalAddress).Tombstone)
            {
                status = OperationStatus.RETRY_LATER;
                // Retain the shared latch (if acquired)
                if (latchOperation == LatchOperation.ReleaseShared)
                {
                    latchOperation = LatchOperation.None;
                }
                goto CreateFailureContext; // Go pending
            }

            // Safe Read-Only Region: Create a record in the mutable region
            else if (logicalAddress >= hlog.HeadAddress)
            {
                goto CreateNewRecord;
            }

            // Disk Region: Need to issue async io requests
            else if (logicalAddress >= hlog.BeginAddress)
            {
                status = OperationStatus.RECORD_ON_DISK;
                // Retain the shared latch (if acquired)
                if (latchOperation == LatchOperation.ReleaseShared)
                {
                    latchOperation = LatchOperation.None;
                }
                goto CreateFailureContext; // Go pending
            }

            // No record exists - create new
            else
            {
                goto CreateNewRecord;
            }

            #endregion

            #region Create new record
            CreateNewRecord:
            {
                recordSize = (logicalAddress < hlog.BeginAddress) ?
                                hlog.GetInitialRecordSize(ref key, ref input) :
                                hlog.GetRecordSize(physicalAddress);
                BlockAllocate(recordSize, out long newLogicalAddress);
                var newPhysicalAddress = hlog.GetPhysicalAddress(newLogicalAddress);
                RecordInfo.WriteInfo(ref hlog.GetInfo(newPhysicalAddress), threadCtx.Value.version,
                               true, false, false,
                               latestLogicalAddress);
                hlog.ShallowCopy(ref key, ref hlog.GetKey(newPhysicalAddress));
                if (logicalAddress < hlog.BeginAddress)
                {
                    functions.InitialUpdater(ref key, ref input, ref hlog.GetValue(newPhysicalAddress));
                    status = OperationStatus.NOTFOUND;
                }
                else if (logicalAddress >= hlog.HeadAddress)
                {
                    if (hlog.GetInfo(physicalAddress).Tombstone)
                    {
                        functions.InitialUpdater(ref key, ref input, ref hlog.GetValue(newPhysicalAddress));
                        status = OperationStatus.NOTFOUND;
                    }
                    else
                    {
                        functions.CopyUpdater(ref key, ref input,
                                                ref hlog.GetValue(physicalAddress),
                                                ref hlog.GetValue(newPhysicalAddress));
                        status = OperationStatus.SUCCESS;
                    }
                }
                else
                {
                    // ah, old record slipped onto disk
                    hlog.GetInfo(newPhysicalAddress).Invalid = true;
                    status = OperationStatus.RETRY_NOW;
                    goto LatchRelease;
                }

                var updatedEntry = default(HashBucketEntry);
                updatedEntry.Tag = tag;
                updatedEntry.Address = newLogicalAddress & Constants.kAddressMask;
                updatedEntry.Pending = entry.Pending;
                updatedEntry.Tentative = false;

                var foundEntry = default(HashBucketEntry);
                foundEntry.word = Interlocked.CompareExchange(
                                        ref bucket->bucket_entries[slot],
                                        updatedEntry.word, entry.word);

                if (foundEntry.word == entry.word)
                {
                    goto LatchRelease;
                }
                else
                {
                    // ah, CAS failed
                    hlog.GetInfo(newPhysicalAddress).Invalid = true;
                    status = OperationStatus.RETRY_NOW;
                    goto LatchRelease;
                }
            }
            #endregion

            #region Create failure context
            CreateFailureContext:
            {
                pendingContext.type = OperationType.RMW;
                pendingContext.key = hlog.GetKeyContainer(ref key);
                pendingContext.input = input;
                pendingContext.userContext = userContext;
                pendingContext.entry.word = entry.word;
                pendingContext.logicalAddress = logicalAddress;
                pendingContext.version = threadCtx.Value.version;
                pendingContext.serialNum = threadCtx.Value.serialNum + 1;
            }
            #endregion

            #region Latch release
            LatchRelease:
            {
                switch (latchOperation)
                {
                    case LatchOperation.ReleaseShared:
                        HashBucket.ReleaseSharedLatch(bucket);
                        break;
                    case LatchOperation.ReleaseExclusive:
                        HashBucket.ReleaseExclusiveLatch(bucket);
                        break;
                    default:
                        break;
                }
            }
            #endregion

            if (status == OperationStatus.RETRY_NOW)
            {
                return InternalRMW(ref key, ref input, ref userContext, ref pendingContext);
            }
            else
            {
                return status;
            }
        }

        /// <summary>
        /// Retries a pending RMW operation. 
        /// </summary>
        /// <param name="ctx">Thread (or session) context under which operation must be executed.</param>
        /// <param name="pendingContext">Internal context of the RMW operation.</param>
        /// <returns>
        /// <list type="table">
        ///     <listheader>
        ///     <term>Value</term>
        ///     <term>Description</term>
        ///     </listheader>
        ///     <item>
        ///     <term>SUCCESS</term>
        ///     <term>The value has been successfully updated(or inserted).</term>
        ///     </item>
        ///     <item>
        ///     <term>RECORD_ON_DISK</term>
        ///     <term>The record corresponding to 'key' is on disk. Issue async IO to retrieve record and retry later.</term>
        ///     </item>
        ///     <item>
        ///     <term>RETRY_LATER</term>
        ///     <term>Cannot  be processed immediately due to system state. Add to pending list and retry later.</term>
        ///     </item>
        /// </list>
        /// </returns>
        internal OperationStatus InternalRetryPendingRMW(
                            FasterExecutionContext ctx,
                            ref PendingContext pendingContext)
        {
            var recordSize = default(int);
            var bucket = default(HashBucket*);
            var slot = default(int);
            var logicalAddress = Constants.kInvalidAddress;
            var physicalAddress = default(long);
            var version = default(int);
            var latestRecordVersion = -1;
            var status = default(OperationStatus);
            var latchOperation = LatchOperation.None;
            ref Key key = ref pendingContext.key.Get();

            var hash = comparer.GetHashCode64(ref key);
            var tag = (ushort)((ulong)hash >> Constants.kHashTagShift);

            if (threadCtx.Value.phase != Phase.REST)
                HeavyEnter(hash);

            #region Trace back for record in in-memory HybridLog
            var entry = default(HashBucketEntry);
            FindOrCreateTag(hash, tag, ref bucket, ref slot, ref entry, hlog.BeginAddress);
            logicalAddress = entry.Address;

            // For simplicity, we don't let RMW operations use read cache
            if (UseReadCache)
                SkipReadCache(ref logicalAddress, ref latestRecordVersion);
            var latestLogicalAddress = logicalAddress;

            if (logicalAddress >= hlog.HeadAddress)
            {
                physicalAddress = hlog.GetPhysicalAddress(logicalAddress);
                if (latestRecordVersion == -1)
                    latestRecordVersion = hlog.GetInfo(physicalAddress).Version;
                if (!comparer.Equals(ref key, ref hlog.GetKey(physicalAddress)))
                {
                    logicalAddress = hlog.GetInfo(physicalAddress).PreviousAddress;
                    TraceBackForKeyMatch(ref key, logicalAddress,
                                            hlog.HeadAddress,
                                            out logicalAddress,
                                            out physicalAddress);
                }
            }
            #endregion

            #region Entry latch operation
            if (threadCtx.Value.phase != Phase.REST)
            {
                if (!((ctx.version < threadCtx.Value.version)
                      ||
                      (threadCtx.Value.phase == Phase.PREPARE)))
                {
                    // Processing a pending (v+1) request
                    version = (threadCtx.Value.version - 1);
                    switch (threadCtx.Value.phase)
                    {
                        case Phase.IN_PROGRESS:
                            {
                                if (latestRecordVersion != -1 && latestRecordVersion <= version)
                                {
                                    if (HashBucket.TryAcquireExclusiveLatch(bucket))
                                    {
                                        // Set to release exclusive latch (default)
                                        latchOperation = LatchOperation.ReleaseExclusive;
                                        goto CreateNewRecord; // Create a (v+1) record
                                    }
                                    else
                                    {
                                        status = OperationStatus.RETRY_LATER;
                                        goto UpdateFailureContext; // Go Pending
                                    }
                                }
                                break; // Normal Processing
                            }
                        case Phase.WAIT_PENDING:
                            {
                                if (latestRecordVersion != -1 && latestRecordVersion <= version)
                                {
                                    if (HashBucket.NoSharedLatches(bucket))
                                    {
                                        goto CreateNewRecord; // Create a (v+1) record
                                    }
                                    else
                                    {
                                        status = OperationStatus.RETRY_LATER;
                                        goto UpdateFailureContext; // Go Pending
                                    }
                                }
                                break; // Normal Processing
                            }
                        case Phase.WAIT_FLUSH:
                            {
                                if (latestRecordVersion != -1 && latestRecordVersion <= version)
                                {
                                    goto CreateNewRecord; // Create a (v+1) record
                                }
                                break; // Normal Processing
                            }
                        default:
                            break;
                    }
                }
            }
            #endregion

            #region Normal processing

            // Mutable Region: Update the record in-place
            if (logicalAddress >= hlog.ReadOnlyAddress)
            {
                if (FoldOverSnapshot)
                {
                    Debug.Assert(hlog.GetInfo(physicalAddress).Version == threadCtx.Value.version);
                }

                if (functions.InPlaceUpdater(ref key, ref pendingContext.input, ref hlog.GetValue(physicalAddress)))
                {
                    status = OperationStatus.SUCCESS;
                    goto LatchRelease;
                }
            }

            // Fuzzy Region: Must go pending due to lost-update anomaly
            else if (logicalAddress >= hlog.SafeReadOnlyAddress)
            {
                status = OperationStatus.RETRY_LATER;
                goto UpdateFailureContext; // Go pending
            }

            // Safe Read-Only Region: Create a record in the mutable region
            else if (logicalAddress >= hlog.HeadAddress)
            {
                goto CreateNewRecord;
            }

            // Disk Region: Need to issue async io requests
            else if (logicalAddress >= hlog.BeginAddress)
            {
                status = OperationStatus.RECORD_ON_DISK;
                goto UpdateFailureContext; // Go pending
            }

            // No record exists - create new
            else
            {
                goto CreateNewRecord;
            }

            #endregion

            #region Create new record in mutable region
            CreateNewRecord:
            {
                recordSize = (logicalAddress < hlog.BeginAddress) ?
                                hlog.GetInitialRecordSize(ref key, ref pendingContext.input) :
                                hlog.GetRecordSize(physicalAddress);
                BlockAllocate(recordSize, out long newLogicalAddress);
                var newPhysicalAddress = hlog.GetPhysicalAddress(newLogicalAddress);
                RecordInfo.WriteInfo(ref hlog.GetInfo(newPhysicalAddress), pendingContext.version,
                               true, false, false,
                               latestLogicalAddress);
                hlog.ShallowCopy(ref key, ref hlog.GetKey(newPhysicalAddress));
                if (logicalAddress < hlog.BeginAddress)
                {
                    functions.InitialUpdater(ref key,
                                             ref pendingContext.input,
                                             ref hlog.GetValue(newPhysicalAddress));
                    status = OperationStatus.NOTFOUND;
                }
                else if (logicalAddress >= hlog.HeadAddress)
                {
                    functions.CopyUpdater(ref key,
                                            ref pendingContext.input,
                                            ref hlog.GetValue(physicalAddress),
                                            ref hlog.GetValue(newPhysicalAddress));
                    status = OperationStatus.SUCCESS;
                }
                else
                {
                    // record slipped onto disk
                    hlog.GetInfo(newPhysicalAddress).Invalid = true;
                    status = OperationStatus.RETRY_NOW;
                    goto LatchRelease;
                }

                var updatedEntry = default(HashBucketEntry);
                updatedEntry.Tag = tag;
                updatedEntry.Address = newLogicalAddress & Constants.kAddressMask;
                updatedEntry.Pending = entry.Pending;
                updatedEntry.Tentative = false;

                var foundEntry = default(HashBucketEntry);
                foundEntry.word = Interlocked.CompareExchange(
                                        ref bucket->bucket_entries[slot],
                                        updatedEntry.word, entry.word);

                if (foundEntry.word == entry.word)
                {
                    goto LatchRelease;
                }
                else
                {
                    // ah, CAS failed
                    hlog.GetInfo(newPhysicalAddress).Invalid = true;
                    status = OperationStatus.RETRY_NOW;
                    goto LatchRelease;
                }
            }
            #endregion

            #region Update failure context
            UpdateFailureContext:
            {
                pendingContext.entry.word = entry.word;
                pendingContext.logicalAddress = logicalAddress;
            }
            #endregion

            #region Latch release
            LatchRelease:
            {
                switch (latchOperation)
                {
                    case LatchOperation.ReleaseExclusive:
                        HashBucket.ReleaseExclusiveLatch(bucket);
                        break;
                    case LatchOperation.ReleaseShared:
                        throw new Exception("Should not release shared latch here!");
                    default:
                        break;
                }
            }
            #endregion

            if (status == OperationStatus.RETRY_NOW)
            {
                return InternalRetryPendingRMW(ctx, ref pendingContext);
            }
            else
            {
                return status;
            }
        }

        /// <summary>
        /// Continue a pending RMW operation with the record retrieved from disk.
        /// </summary>
        /// <param name="ctx">thread (or session) context under which operation must be executed.</param>
        /// <param name="request">record read from the disk.</param>
        /// <param name="pendingContext">internal context for the pending RMW operation</param>
        /// <returns>
        /// <list type="table">
        ///     <listheader>
        ///     <term>Value</term>
        ///     <term>Description</term>
        ///     </listheader>
        ///     <item>
        ///     <term>SUCCESS</term>
        ///     <term>The value has been successfully updated(or inserted).</term>
        ///     </item>
        ///     <item>
        ///     <term>RECORD_ON_DISK</term>
        ///     <term>The record corresponding to 'key' is on disk. Issue async IO to retrieve record and retry later.</term>
        ///     </item>
        ///     <item>
        ///     <term>RETRY_LATER</term>
        ///     <term>Cannot  be processed immediately due to system state. Add to pending list and retry later.</term>
        ///     </item>
        /// </list>
        /// </returns>
        internal OperationStatus InternalContinuePendingRMW(
                                    FasterExecutionContext ctx,
                                    AsyncIOContext<Key, Value> request,
                                    ref PendingContext pendingContext)
        {
            var recordSize = default(int);
            var bucket = default(HashBucket*);
            var slot = default(int);
            var logicalAddress = Constants.kInvalidAddress;
            var physicalAddress = default(long);
            var status = default(OperationStatus);
            var latestRecordVersion = default(int);
            ref Key key = ref pendingContext.key.Get();

            var hash = comparer.GetHashCode64(ref key);
            var tag = (ushort)((ulong)hash >> Constants.kHashTagShift);

            #region Trace Back for Record on In-Memory HybridLog
            var entry = default(HashBucketEntry);
            FindOrCreateTag(hash, tag, ref bucket, ref slot, ref entry, hlog.BeginAddress);
            logicalAddress = entry.Address;

            // For simplicity, we don't let RMW operations use read cache
            if (UseReadCache)
                SkipReadCache(ref logicalAddress, ref latestRecordVersion);
            var latestLogicalAddress = logicalAddress;

            if (logicalAddress >= hlog.HeadAddress)
            {
                physicalAddress = hlog.GetPhysicalAddress(logicalAddress);
                if (!comparer.Equals(ref key, ref hlog.GetKey(physicalAddress)))
                {
                    logicalAddress = hlog.GetInfo(physicalAddress).PreviousAddress;
                    TraceBackForKeyMatch(ref key,
                                            logicalAddress,
                                            hlog.HeadAddress,
                                            out logicalAddress,
                                            out physicalAddress);
                }
            }
            #endregion

            var previousFirstRecordAddress = pendingContext.entry.Address;
            if (logicalAddress > previousFirstRecordAddress)
            {
                goto Retry;
            }

            #region Create record in mutable region
            if ((request.logicalAddress < hlog.BeginAddress) || (hlog.GetInfoFromBytePointer(request.record.GetValidPointer()).Tombstone))
            {
                recordSize = hlog.GetInitialRecordSize(ref key, ref pendingContext.input);
            }
            else
            {
                physicalAddress = (long)request.record.GetValidPointer();
                recordSize = hlog.GetRecordSize(physicalAddress);
            }
            BlockAllocate(recordSize, out long newLogicalAddress);
            var newPhysicalAddress = hlog.GetPhysicalAddress(newLogicalAddress);
            RecordInfo.WriteInfo(ref hlog.GetInfo(newPhysicalAddress), ctx.version,
                           true, false, false,
                           latestLogicalAddress);
            hlog.ShallowCopy(ref key, ref hlog.GetKey(newPhysicalAddress));
            if ((request.logicalAddress < hlog.BeginAddress) || (hlog.GetInfoFromBytePointer(request.record.GetValidPointer()).Tombstone))
            {
                functions.InitialUpdater(ref key,
                                         ref pendingContext.input,
                                         ref hlog.GetValue(newPhysicalAddress));
                status = OperationStatus.NOTFOUND;
            }
            else
            {
                functions.CopyUpdater(ref key,
                                      ref pendingContext.input,
                                      ref hlog.GetContextRecordValue(ref request),
                                      ref hlog.GetValue(newPhysicalAddress));
                status = OperationStatus.SUCCESS;
            }

            var updatedEntry = default(HashBucketEntry);
            updatedEntry.Tag = tag;
            updatedEntry.Address = newLogicalAddress & Constants.kAddressMask;
            updatedEntry.Pending = entry.Pending;
            updatedEntry.Tentative = false;

            var foundEntry = default(HashBucketEntry);
            foundEntry.word = Interlocked.CompareExchange(
                                        ref bucket->bucket_entries[slot],
                                        updatedEntry.word, entry.word);

            if (foundEntry.word == entry.word)
            {
                return status;
            }
            else
            {
                hlog.GetInfo(newPhysicalAddress).Invalid = true;
                goto Retry;
            }
            #endregion

            Retry:
            return InternalRetryPendingRMW(ctx, ref pendingContext);
        }

        #endregion

        #region Delete Operation

        /// <summary>
        /// Delete operation. Replaces the value corresponding to 'key' with tombstone.
        /// If at head, tries to remove item from hash chain
        /// </summary>
        /// <param name="key">Key of the record to be deleted.</param>
        /// <param name="userContext">User context for the operation, in case it goes pending.</param>
        /// <param name="pendingContext">Pending context used internally to store the context of the operation.</param>
        /// <returns>
        /// <list type="table">
        ///     <listheader>
        ///     <term>Value</term>
        ///     <term>Description</term>
        ///     </listheader>
        ///     <item>
        ///     <term>SUCCESS</term>
        ///     <term>The value has been successfully deleted</term>
        ///     </item>
        ///     <item>
        ///     <term>RETRY_LATER</term>
        ///     <term>Cannot  be processed immediately due to system state. Add to pending list and retry later</term>
        ///     </item>
        ///     <item>
        ///     <term>CPR_SHIFT_DETECTED</term>
        ///     <term>A shift in version has been detected. Synchronize immediately to avoid violating CPR consistency.</term>
        ///     </item>
        /// </list>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal OperationStatus InternalDelete(
                            ref Key key,
                            ref Context userContext,
                            ref PendingContext pendingContext)
        {
            var status = default(OperationStatus);
            var bucket = default(HashBucket*);
            var slot = default(int);
            var logicalAddress = Constants.kInvalidAddress;
            var physicalAddress = default(long);
            var latchOperation = default(LatchOperation);
            var version = default(int);
            var latestRecordVersion = -1;

            var hash = comparer.GetHashCode64(ref key);
            var tag = (ushort)((ulong)hash >> Constants.kHashTagShift);

            if (threadCtx.Value.phase != Phase.REST)
                HeavyEnter(hash);

            #region Trace back for record in in-memory HybridLog
            var entry = default(HashBucketEntry);
            var tagExists = FindTag(hash, tag, ref bucket, ref slot, ref entry);
            if (!tagExists)
                return OperationStatus.NOTFOUND;

            logicalAddress = entry.Address;

            if (UseReadCache)
                SkipAndInvalidateReadCache(ref logicalAddress, ref latestRecordVersion, ref key);
            var latestLogicalAddress = logicalAddress;

            if (logicalAddress >= hlog.ReadOnlyAddress)
            {
                physicalAddress = hlog.GetPhysicalAddress(logicalAddress);
                if (latestRecordVersion == -1)
                    latestRecordVersion = hlog.GetInfo(physicalAddress).Version;
                if (!comparer.Equals(ref key, ref hlog.GetKey(physicalAddress)))
                {
                    logicalAddress = hlog.GetInfo(physicalAddress).PreviousAddress;
                    TraceBackForKeyMatch(ref key,
                                        logicalAddress,
                                        hlog.ReadOnlyAddress,
                                        out logicalAddress,
                                        out physicalAddress);
                }
            }
            #endregion

            // NO optimization for most common case
            //if (threadCtx.Value.phase == Phase.REST && logicalAddress >= hlog.ReadOnlyAddress)
            //{
            //    hlog.GetInfo(physicalAddress).Tombstone = true;
            //    return OperationStatus.SUCCESS;
            //}

            #region Entry latch operation
            if (threadCtx.Value.phase != Phase.REST)
            {
                switch (threadCtx.Value.phase)
                {
                    case Phase.PREPARE:
                        {
                            version = threadCtx.Value.version;
                            if (HashBucket.TryAcquireSharedLatch(bucket))
                            {
                                // Set to release shared latch (default)
                                latchOperation = LatchOperation.ReleaseShared;
                                if (latestRecordVersion != -1 && latestRecordVersion > version)
                                {
                                    status = OperationStatus.CPR_SHIFT_DETECTED;
                                    goto CreatePendingContext; // Pivot Thread
                                }
                                break; // Normal Processing
                            }
                            else
                            {
                                status = OperationStatus.CPR_SHIFT_DETECTED;
                                goto CreatePendingContext; // Pivot Thread
                            }
                        }
                    case Phase.IN_PROGRESS:
                        {
                            version = (threadCtx.Value.version - 1);
                            if (latestRecordVersion != -1 && latestRecordVersion <= version)
                            {
                                if (HashBucket.TryAcquireExclusiveLatch(bucket))
                                {
                                    // Set to release exclusive latch (default)
                                    latchOperation = LatchOperation.ReleaseExclusive;
                                    goto CreateNewRecord; // Create a (v+1) record
                                }
                                else
                                {
                                    status = OperationStatus.RETRY_LATER;
                                    goto CreatePendingContext; // Go Pending
                                }
                            }
                            break; // Normal Processing
                        }
                    case Phase.WAIT_PENDING:
                        {
                            version = (threadCtx.Value.version - 1);
                            if (latestRecordVersion != -1 && latestRecordVersion <= version)
                            {
                                if (HashBucket.NoSharedLatches(bucket))
                                {
                                    goto CreateNewRecord; // Create a (v+1) record
                                }
                                else
                                {
                                    status = OperationStatus.RETRY_LATER;
                                    goto CreatePendingContext; // Go Pending
                                }
                            }
                            break; // Normal Processing
                        }
                    case Phase.WAIT_FLUSH:
                        {
                            version = (threadCtx.Value.version - 1);
                            if (latestRecordVersion != -1 && latestRecordVersion <= version)
                            {
                                goto CreateNewRecord; // Create a (v+1) record
                            }
                            break; // Normal Processing
                        }
                    default:
                        break;
                }
            }
            #endregion

            Debug.Assert(latestRecordVersion <= threadCtx.Value.version);

            #region Normal processing

            // Record is in memory, try to update hash chain and completely elide record
            // only if previous address points to invalid address
            if (logicalAddress >= hlog.ReadOnlyAddress)
            {
                if (entry.Address == logicalAddress && hlog.GetInfo(physicalAddress).PreviousAddress < hlog.BeginAddress)
                {
                    var updatedEntry = default(HashBucketEntry);
                    updatedEntry.Tag = 0;
                    if (hlog.GetInfo(physicalAddress).PreviousAddress == Constants.kTempInvalidAddress)
                        updatedEntry.Address = Constants.kInvalidAddress;
                    else
                        updatedEntry.Address = hlog.GetInfo(physicalAddress).PreviousAddress;
                    updatedEntry.Pending = entry.Pending;
                    updatedEntry.Tentative = false;

                    if (entry.word == Interlocked.CompareExchange(ref bucket->bucket_entries[slot], updatedEntry.word, entry.word))
                    {
                        // Apply tombstone bit to the record
                        hlog.GetInfo(physicalAddress).Tombstone = true;

                        if (WriteDefaultOnDelete)
                        {
                            // Write default value
                            // Ignore return value, the record is already marked
                            Value v = default(Value);
                            functions.ConcurrentWriter(ref hlog.GetKey(physicalAddress), ref v, ref hlog.GetValue(physicalAddress));
                        }

                        status = OperationStatus.SUCCESS;
                        goto LatchRelease; // Release shared latch (if acquired)
                    }
                }
            }

            // Mutable Region: Update the record in-place
            if (logicalAddress >= hlog.ReadOnlyAddress)
            {
                hlog.GetInfo(physicalAddress).Tombstone = true;

                if (WriteDefaultOnDelete)
                {
                    // Write default value
                    // Ignore return value, the record is already marked
                    Value v = default(Value);
                    functions.ConcurrentWriter(ref hlog.GetKey(physicalAddress), ref v, ref hlog.GetValue(physicalAddress));
                }

                status = OperationStatus.SUCCESS;
                goto LatchRelease; // Release shared latch (if acquired)
            }

            // All other regions: Create a record in the mutable region
            #endregion

            #region Create new record in the mutable region
            CreateNewRecord:
            {
                var value = default(Value);
                // Immutable region or new record
                // Allocate default record size for tombstone
                var recordSize = hlog.GetRecordSize(ref key, ref value);
                BlockAllocate(recordSize, out long newLogicalAddress);
                var newPhysicalAddress = hlog.GetPhysicalAddress(newLogicalAddress);
                RecordInfo.WriteInfo(ref hlog.GetInfo(newPhysicalAddress),
                               threadCtx.Value.version,
                               true, true, false,
                               latestLogicalAddress);
                hlog.ShallowCopy(ref key, ref hlog.GetKey(newPhysicalAddress));

                var updatedEntry = default(HashBucketEntry);
                updatedEntry.Tag = tag;
                updatedEntry.Address = newLogicalAddress & Constants.kAddressMask;
                updatedEntry.Pending = entry.Pending;
                updatedEntry.Tentative = false;

                var foundEntry = default(HashBucketEntry);
                foundEntry.word = Interlocked.CompareExchange(
                                        ref bucket->bucket_entries[slot],
                                        updatedEntry.word, entry.word);

                if (foundEntry.word == entry.word)
                {
                    status = OperationStatus.SUCCESS;
                    goto LatchRelease;
                }
                else
                {
                    hlog.GetInfo(newPhysicalAddress).Invalid = true;
                    status = OperationStatus.RETRY_NOW;
                    goto LatchRelease;
                }
            }
            #endregion

            #region Create pending context
            CreatePendingContext:
            {
                pendingContext.type = OperationType.DELETE;
                pendingContext.key = hlog.GetKeyContainer(ref key);
                pendingContext.userContext = userContext;
                pendingContext.entry.word = entry.word;
                pendingContext.logicalAddress = logicalAddress;
                pendingContext.version = threadCtx.Value.version;
                pendingContext.serialNum = threadCtx.Value.serialNum + 1;
            }
            #endregion

            #region Latch release
            LatchRelease:
            {
                switch (latchOperation)
                {
                    case LatchOperation.ReleaseShared:
                        HashBucket.ReleaseSharedLatch(bucket);
                        break;
                    case LatchOperation.ReleaseExclusive:
                        HashBucket.ReleaseExclusiveLatch(bucket);
                        break;
                    default:
                        break;
                }
            }
            #endregion

            if (status == OperationStatus.RETRY_NOW)
            {
                return InternalDelete(ref key, ref userContext, ref pendingContext);
            }
            else
            {
                return status;
            }
        }

        #endregion

        #region ContainsKeyInMemory
        /// <summary>
        /// Experimental feature
        /// Checks whether specified record is present in memory
        /// (between HeadAddress and tail, or between fromAddress
        /// and tail)
        /// </summary>
        /// <param name="key">Key of the record.</param>
        /// <param name="fromAddress">Look until this address</param>
        /// <returns>Status</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Status ContainsKeyInMemory(ref Key key, long fromAddress = -1)
        {
            if (fromAddress == -1)
                fromAddress = hlog.HeadAddress;
            else
                Debug.Assert(fromAddress >= hlog.HeadAddress);

            var bucket = default(HashBucket*);
            var slot = default(int);
            var logicalAddress = Constants.kInvalidAddress;
            var physicalAddress = default(long);
            var latestRecordVersion = -1;

            var hash = comparer.GetHashCode64(ref key);
            var tag = (ushort)((ulong)hash >> Constants.kHashTagShift);

            if (threadCtx.Value.phase != Phase.REST)
                HeavyEnter(hash);

            HashBucketEntry entry = default(HashBucketEntry);
            var tagExists = FindTag(hash, tag, ref bucket, ref slot, ref entry);

            if (tagExists)
            {
                logicalAddress = entry.Address;

                if (UseReadCache)
                    SkipReadCache(ref logicalAddress, ref latestRecordVersion);

                if (logicalAddress >= fromAddress)
                {
                    physicalAddress = hlog.GetPhysicalAddress(logicalAddress);
                    if (latestRecordVersion == -1)
                        latestRecordVersion = hlog.GetInfo(physicalAddress).Version;

                    if (!comparer.Equals(ref key, ref hlog.GetKey(physicalAddress)))
                    {
                        logicalAddress = hlog.GetInfo(physicalAddress).PreviousAddress;
                        TraceBackForKeyMatch(ref key,
                                                logicalAddress,
                                                fromAddress,
                                                out logicalAddress,
                                                out physicalAddress);
                    }

                    if (logicalAddress < fromAddress)
                        return Status.NOTFOUND;
                    else
                        return Status.OK;
                }
                else
                    return Status.NOTFOUND;
            }
            else
            {
                // no tag found
                return Status.NOTFOUND;
            }
        }
        #endregion

        #region Helper Functions

        /// <summary>
        /// Performs appropriate handling based on the internal failure status of the trial.
        /// </summary>
        /// <param name="ctx">Thread (or session) context under which operation was tried to execute.</param>
        /// <param name="pendingContext">Internal context of the operation.</param>
        /// <param name="status">Internal status of the trial.</param>
        /// <returns>
        /// <list type="table">
        ///     <listheader>
        ///     <term>Value</term>
        ///     <term>Description</term>
        ///     </listheader>
        ///     <item>
        ///     <term>OK</term>
        ///     <term>The operation has been completed successfully.</term>
        ///     </item>
        ///     <item>
        ///     <term>PENDING</term>
        ///     <term>The operation is still pending and will callback when done.</term>
        ///     </item>
        /// </list>
        /// </returns>
        internal Status HandleOperationStatus(
                    FasterExecutionContext ctx,
                    PendingContext pendingContext,
                    OperationStatus status)
        {
            if (status == OperationStatus.CPR_SHIFT_DETECTED)
            {
                #region Epoch Synchronization
                var version = ctx.version;
                Debug.Assert(threadCtx.Value.version == version);
                Debug.Assert(threadCtx.Value.phase == Phase.PREPARE);
                Refresh();
                Debug.Assert(threadCtx.Value.version == version + 1);
                Debug.Assert(threadCtx.Value.phase == Phase.IN_PROGRESS);

                pendingContext.version = threadCtx.Value.version;
                #endregion

                #region Retry as (v+1) Operation
                var internalStatus = default(OperationStatus);
                switch (pendingContext.type)
                {
                    case OperationType.READ:
                        internalStatus = InternalRead(ref pendingContext.key.Get(),
                                                      ref pendingContext.input,
                                                      ref pendingContext.output,
                                                      ref pendingContext.userContext,
                                                      ref pendingContext);
                        break;
                    case OperationType.UPSERT:
                        internalStatus = InternalUpsert(ref pendingContext.key.Get(),
                                                        ref pendingContext.value.Get(),
                                                        ref pendingContext.userContext,
                                                        ref pendingContext);
                        break;
                    case OperationType.DELETE:
                        internalStatus = InternalDelete(ref pendingContext.key.Get(),
                                                        ref pendingContext.userContext,
                                                        ref pendingContext);
                        break;
                    case OperationType.RMW:
                        internalStatus = InternalRetryPendingRMW(threadCtx.Value, ref pendingContext);
                        break;
                }

                Debug.Assert(internalStatus != OperationStatus.CPR_SHIFT_DETECTED);
                status = internalStatus;
                #endregion
            }

            if (status == OperationStatus.SUCCESS || status == OperationStatus.NOTFOUND)
            {
                return (Status)status;
            }
            else if (status == OperationStatus.RECORD_ON_DISK)
            {
                //Add context to dictionary
                pendingContext.id = ctx.totalPending++;
                ctx.ioPendingRequests.Add(pendingContext.id, pendingContext);

                // Issue asynchronous I/O request
                AsyncIOContext<Key, Value> request = default(AsyncIOContext<Key, Value>);
                request.id = pendingContext.id;
                request.request_key = pendingContext.key;
                request.logicalAddress = pendingContext.logicalAddress;
                request.callbackQueue = ctx.readyResponses;
                request.record = default(SectorAlignedMemory);
                hlog.AsyncGetFromDisk(pendingContext.logicalAddress,
                                 hlog.GetAverageRecordSize(),
                                 request);

                return Status.PENDING;
            }
            else if (status == OperationStatus.RETRY_LATER)
            {
                ctx.retryRequests.Enqueue(pendingContext);
                return Status.PENDING;
            }
            else
            {
                return Status.ERROR;
            }
        }

        private void AcquireSharedLatch(Key key)
        {
            var bucket = default(HashBucket*);
            var slot = default(int);
            var hash = comparer.GetHashCode64(ref key);
            var tag = (ushort)((ulong)hash >> Constants.kHashTagShift);
            var entry = default(HashBucketEntry);
            FindOrCreateTag(hash, tag, ref bucket, ref slot, ref entry, hlog.BeginAddress);
            HashBucket.TryAcquireSharedLatch(bucket);
        }

        private void ReleaseSharedLatch(Key key)
        {
            var bucket = default(HashBucket*);
            var slot = default(int);
            var hash = comparer.GetHashCode64(ref key);
            var tag = (ushort)((ulong)hash >> Constants.kHashTagShift);
            var entry = default(HashBucketEntry);
            FindOrCreateTag(hash, tag, ref bucket, ref slot, ref entry, hlog.BeginAddress);
            HashBucket.ReleaseSharedLatch(bucket);
        }

        private void HeavyEnter(long hash)
        {
            if (threadCtx.Value.phase == Phase.GC)
                GarbageCollectBuckets(hash);
            if (threadCtx.Value.phase == Phase.PREPARE_GROW)
            {
                // We spin-wait as a simplification
                // Could instead do a "heavy operation" here
                while (_systemState.phase != Phase.IN_PROGRESS_GROW)
                    Thread.SpinWait(100);
                Refresh();
            }
            if (threadCtx.Value.phase == Phase.IN_PROGRESS_GROW)
            {
                SplitBuckets(hash);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BlockAllocate(int recordSize, out long logicalAddress)
        {
            while ((logicalAddress = hlog.TryAllocate(recordSize)) == 0)
            {
                InternalRefresh();
                Thread.Yield();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BlockAllocateReadCache(int recordSize, out long logicalAddress)
        {
            while ((logicalAddress = readcache.TryAllocate(recordSize)) == 0)
            {
                InternalRefresh();
                Thread.Yield();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TraceBackForKeyMatch(
                                    ref Key key,
                                    long fromLogicalAddress,
                                    long minOffset,
                                    out long foundLogicalAddress,
                                    out long foundPhysicalAddress)
        {
            foundLogicalAddress = fromLogicalAddress;
            while (foundLogicalAddress >= minOffset)
            {
                foundPhysicalAddress = hlog.GetPhysicalAddress(foundLogicalAddress);
                if (comparer.Equals(ref key, ref hlog.GetKey(foundPhysicalAddress)))
                {
                    return true;
                }
                else
                {
                    foundLogicalAddress = hlog.GetInfo(foundPhysicalAddress).PreviousAddress;
                    Debug.WriteLine("Tracing back");
                    continue;
                }
            }
            foundPhysicalAddress = Constants.kInvalidAddress;
            return false;
        }
        #endregion

        #region Garbage Collection
        private long[] gcStatus;
        private long numPendingChunksToBeGCed;

        private void GarbageCollectBuckets(long hash, bool force = false)
        {
            if (numPendingChunksToBeGCed == 0)
            {
                InternalRefresh();
                return;
            }

            long masked_bucket_index = hash & state[resizeInfo.version].size_mask;
            int offset = (int)(masked_bucket_index >> Constants.kSizeofChunkBits);

            int numChunks = (int)(state[resizeInfo.version].size / Constants.kSizeofChunk);
            if (numChunks == 0) numChunks = 1; // at least one chunk

            if (!Utility.IsPowerOfTwo(numChunks))
            {
                throw new Exception("Invalid number of chunks: " + numChunks);
            }

            for (int i = offset; i < offset + numChunks; i++)
            {
                if (0 == Interlocked.CompareExchange(ref gcStatus[i & (numChunks - 1)], 1, 0))
                {
                    int version = resizeInfo.version;
                    long chunkSize = state[version].size / numChunks;
                    long ptr = chunkSize * (i & (numChunks - 1));

                    HashBucket* src_start = state[version].tableAligned + ptr;
                    // CleanBucket(src_start, chunkSize);

                    // GC for chunk is done
                    gcStatus[i & (numChunks - 1)] = 2;

                    if (Interlocked.Decrement(ref numPendingChunksToBeGCed) == 0)
                    {
                        long context = 0;
                        GlobalMoveToNextState(_systemState, SystemState.Make(Phase.REST, _systemState.version), ref context);
                        return;
                    }
                    if (!force)
                        break;

                    InternalRefresh();
                }
            }
        }

        private void CleanBucket(HashBucket* _src_start, long chunkSize)
        {
            HashBucketEntry entry = default(HashBucketEntry);

            for (int i = 0; i < chunkSize; i++)
            {
                var src_start = _src_start + i;

                do
                {
                    for (int index = 0; index < Constants.kOverflowBucketIndex; ++index)
                    {
                        entry.word = *(((long*)src_start) + index);
                        if (entry.Address != Constants.kInvalidAddress && entry.Address != Constants.kTempInvalidAddress && entry.Address < hlog.BeginAddress)
                        {
                            Interlocked.CompareExchange(ref *(((long*)src_start) + index), Constants.kInvalidAddress, entry.word);
                        }
                    }

                    if (*(((long*)src_start) + Constants.kOverflowBucketIndex) == 0) break;
                    src_start = (HashBucket*)overflowBucketsAllocator.GetPhysicalAddress(*(((long*)src_start) + Constants.kOverflowBucketIndex));
                } while (true);
            }
        }
        #endregion

        #region Split Index
        private void SplitBuckets(long hash)
        {
            long masked_bucket_index = hash & state[1 - resizeInfo.version].size_mask;
            int offset = (int)(masked_bucket_index >> Constants.kSizeofChunkBits);

            int numChunks = (int)(state[1 - resizeInfo.version].size / Constants.kSizeofChunk);
            if (numChunks == 0) numChunks = 1; // at least one chunk


            if (!Utility.IsPowerOfTwo(numChunks))
            {
                throw new Exception("Invalid number of chunks: " + numChunks);
            }
            for (int i = offset; i < offset + numChunks; i++)
            {
                if (0 == Interlocked.CompareExchange(ref splitStatus[i & (numChunks - 1)], 1, 0))
                {
                    long chunkSize = state[1 - resizeInfo.version].size / numChunks;
                    long ptr = chunkSize * (i & (numChunks - 1));

                    HashBucket* src_start = state[1 - resizeInfo.version].tableAligned + ptr;
                    HashBucket* dest_start0 = state[resizeInfo.version].tableAligned + ptr;
                    HashBucket* dest_start1 = state[resizeInfo.version].tableAligned + state[1 - resizeInfo.version].size + ptr;

                    SplitChunk(src_start, dest_start0, dest_start1, chunkSize);

                    // split for chunk is done
                    splitStatus[i & (numChunks - 1)] = 2;

                    if (Interlocked.Decrement(ref numPendingChunksToBeSplit) == 0)
                    {
                        // GC old version of hash table
                        state[1 - resizeInfo.version] = default(InternalHashTable);

                        long context = 0;
                        GlobalMoveToNextState(_systemState, SystemState.Make(Phase.REST, _systemState.version), ref context);
                        return;
                    }
                    break;
                }
            }

            while (Interlocked.Read(ref splitStatus[offset & (numChunks - 1)]) == 1)
            {

            }

        }

        private void SplitChunk(
                    HashBucket* _src_start,
                    HashBucket* _dest_start0,
                    HashBucket* _dest_start1,
                    long chunkSize)
        {
            for (int i = 0; i < chunkSize; i++)
            {
                var src_start = _src_start + i;

                long* left = (long*)(_dest_start0 + i);
                long* right = (long*)(_dest_start1 + i);
                long* left_end = left + Constants.kOverflowBucketIndex;
                long* right_end = right + Constants.kOverflowBucketIndex;

                HashBucketEntry entry = default(HashBucketEntry);
                do
                {
                    for (int index = 0; index < Constants.kOverflowBucketIndex; ++index)
                    {
                        entry.word = *(((long*)src_start) + index);
                        if (Constants.kInvalidEntry == entry.word)
                        {
                            continue;
                        }

                        var logicalAddress = entry.Address;
                        if (logicalAddress >= hlog.HeadAddress)
                        {
                            var physicalAddress = hlog.GetPhysicalAddress(logicalAddress);
                            var hash = comparer.GetHashCode64(ref hlog.GetKey(physicalAddress));
                            if ((hash & state[resizeInfo.version].size_mask) >> (state[resizeInfo.version].size_bits - 1) == 0)
                            {
                                // Insert in left
                                if (left == left_end)
                                {
                                    var new_bucket = (HashBucket*)overflowBucketsAllocator.Allocate();
                                    *left = (long)new_bucket;
                                    left = (long*)new_bucket;
                                    left_end = left + Constants.kOverflowBucketIndex;
                                }

                                *left = entry.word;
                                left++;

                                // Insert previous address in right
                                entry.Address = TraceBackForOtherChainStart(hlog.GetInfo(physicalAddress).PreviousAddress, 1);
                                if (entry.Address != Constants.kInvalidAddress)
                                {
                                    if (right == right_end)
                                    {
                                        var new_bucket = (HashBucket*)overflowBucketsAllocator.Allocate();
                                        *right = (long)new_bucket;
                                        right = (long*)new_bucket;
                                        right_end = right + Constants.kOverflowBucketIndex;
                                    }

                                    *right = entry.word;
                                    right++;
                                }
                            }
                            else
                            {
                                // Insert in right
                                if (right == right_end)
                                {
                                    var new_bucket = (HashBucket*)overflowBucketsAllocator.Allocate();
                                    *right = (long)new_bucket;
                                    right = (long*)new_bucket;
                                    right_end = right + Constants.kOverflowBucketIndex;
                                }

                                *right = entry.word;
                                right++;

                                // Insert previous address in left
                                entry.Address = TraceBackForOtherChainStart(hlog.GetInfo(physicalAddress).PreviousAddress, 0);
                                if (entry.Address != Constants.kInvalidAddress)
                                {
                                    if (left == left_end)
                                    {
                                        var new_bucket = (HashBucket*)overflowBucketsAllocator.Allocate();
                                        *left = (long)new_bucket;
                                        left = (long*)new_bucket;
                                        left_end = left + Constants.kOverflowBucketIndex;
                                    }

                                    *left = entry.word;
                                    left++;
                                }
                            }
                        }
                        else
                        {
                            // Insert in both new locations

                            // Insert in left
                            if (left == left_end)
                            {
                                var new_bucket = (HashBucket*)overflowBucketsAllocator.Allocate();
                                *left = (long)new_bucket;
                                left = (long*)new_bucket;
                                left_end = left + Constants.kOverflowBucketIndex;
                            }

                            *left = entry.word;
                            left++;

                            // Insert in right
                            if (right == right_end)
                            {
                                var new_bucket = (HashBucket*)overflowBucketsAllocator.Allocate();
                                *right = (long)new_bucket;
                                right = (long*)new_bucket;
                                right_end = right + Constants.kOverflowBucketIndex;
                            }

                            *right = entry.word;
                            right++;
                        }
                    }

                    if (*(((long*)src_start) + Constants.kOverflowBucketIndex) == 0) break;
                    src_start = (HashBucket*)overflowBucketsAllocator.GetPhysicalAddress(*(((long*)src_start) + Constants.kOverflowBucketIndex));
                } while (true);
            }
        }

        private long TraceBackForOtherChainStart(long logicalAddress, int bit)
        {
            while (logicalAddress >= hlog.HeadAddress)
            {
                var physicalAddress = hlog.GetPhysicalAddress(logicalAddress);
                var hash = comparer.GetHashCode64(ref hlog.GetKey(physicalAddress));
                if ((hash & state[resizeInfo.version].size_mask) >> (state[resizeInfo.version].size_bits - 1) == bit)
                {
                    return logicalAddress;
                }
                logicalAddress = hlog.GetInfo(physicalAddress).PreviousAddress;
            }
            return logicalAddress;
        }
        #endregion

        #region Read Cache
        private bool ReadFromCache(ref Key key, ref long logicalAddress, ref long physicalAddress, ref int latestRecordVersion)
        {
            HashBucketEntry entry = default(HashBucketEntry);
            entry.word = logicalAddress;
            if (!entry.ReadCache) return false;

            physicalAddress = readcache.GetPhysicalAddress(logicalAddress & ~Constants.kReadCacheBitMask);
            latestRecordVersion = readcache.GetInfo(physicalAddress).Version;

            while (true)
            {
                if (!readcache.GetInfo(physicalAddress).Invalid && comparer.Equals(ref key, ref readcache.GetKey(physicalAddress)))
                {
                    if ((logicalAddress & ~Constants.kReadCacheBitMask) >= readcache.SafeReadOnlyAddress)
                    {
                        return true;
                    }
                    Debug.Assert((logicalAddress & ~Constants.kReadCacheBitMask) >= readcache.SafeHeadAddress);
                    // TODO: copy to tail of read cache
                    // and return new cache entry
                }

                logicalAddress = readcache.GetInfo(physicalAddress).PreviousAddress;
                entry.word = logicalAddress;
                if (!entry.ReadCache) break;
                physicalAddress = readcache.GetPhysicalAddress(logicalAddress & ~Constants.kReadCacheBitMask);
            }
            physicalAddress = 0;
            return false;
        }

        private void SkipReadCache(ref long logicalAddress, ref int latestRecordVersion)
        {
            HashBucketEntry entry = default(HashBucketEntry);
            entry.word = logicalAddress;
            if (!entry.ReadCache) return;

            var physicalAddress = readcache.GetPhysicalAddress(logicalAddress & ~Constants.kReadCacheBitMask);
            latestRecordVersion = readcache.GetInfo(physicalAddress).Version;

            while (true)
            {
                logicalAddress = readcache.GetInfo(physicalAddress).PreviousAddress;
                entry.word = logicalAddress;
                if (!entry.ReadCache) return;
                physicalAddress = readcache.GetPhysicalAddress(logicalAddress & ~Constants.kReadCacheBitMask);
            }
        }

        private void SkipAndInvalidateReadCache(ref long logicalAddress, ref int latestRecordVersion, ref Key key)
        {
            HashBucketEntry entry = default(HashBucketEntry);
            entry.word = logicalAddress;
            if (!entry.ReadCache) return;

            var physicalAddress = readcache.GetPhysicalAddress(logicalAddress & ~Constants.kReadCacheBitMask);
            latestRecordVersion = readcache.GetInfo(physicalAddress).Version;

            while (true)
            {
                // Invalidate read cache entry if key found
                if (comparer.Equals(ref key, ref readcache.GetKey(physicalAddress)))
                {
                    readcache.GetInfo(physicalAddress).Invalid = true;
                }

                logicalAddress = readcache.GetInfo(physicalAddress).PreviousAddress;
                entry.word = logicalAddress;
                if (!entry.ReadCache) return;
                physicalAddress = readcache.GetPhysicalAddress(logicalAddress & ~Constants.kReadCacheBitMask);
            }
        }

        private void ReadCacheEvict(long fromHeadAddress, long toHeadAddress)
        {
            var bucket = default(HashBucket*);
            var slot = default(int);
            var logicalAddress = Constants.kInvalidAddress;
            var physicalAddress = default(long);

            HashBucketEntry entry = default(HashBucketEntry);
            logicalAddress = fromHeadAddress;

            while (logicalAddress < toHeadAddress)
            {
                physicalAddress = readcache.GetPhysicalAddress(logicalAddress);
                var recordSize = readcache.GetRecordSize(physicalAddress);
                ref RecordInfo info = ref readcache.GetInfo(physicalAddress);
                if (!info.Invalid)
                {
                    ref Key key = ref readcache.GetKey(physicalAddress);
                    entry.word = info.PreviousAddress;
                    if (!entry.ReadCache)
                    {
                        var hash = comparer.GetHashCode64(ref key);
                        var tag = (ushort)((ulong)hash >> Constants.kHashTagShift);

                        entry = default(HashBucketEntry);
                        var tagExists = FindTag(hash, tag, ref bucket, ref slot, ref entry);
                        while (tagExists && entry.ReadCache)
                        {
                            var updatedEntry = default(HashBucketEntry);
                            updatedEntry.Tag = tag;
                            updatedEntry.Address = info.PreviousAddress;
                            updatedEntry.Pending = entry.Pending;
                            updatedEntry.Tentative = false;

                            if (entry.word == Interlocked.CompareExchange
                                (ref bucket->bucket_entries[slot], updatedEntry.word, entry.word))
                                break;

                            tagExists = FindTag(hash, tag, ref bucket, ref slot, ref entry);
                        }
                    }
                }
                logicalAddress += recordSize;
                if ((logicalAddress & readcache.PageSizeMask) + recordSize > readcache.PageSize)
                {
                    logicalAddress = (1 + (logicalAddress >> readcache.LogPageSizeBits)) << readcache.LogPageSizeBits;
                    continue;
                }
            }
        }
        #endregion
    }
}
