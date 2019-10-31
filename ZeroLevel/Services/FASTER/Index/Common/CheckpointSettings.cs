﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.


using System;

namespace FASTER.core
{
    /// <summary>
    /// Checkpoint type
    /// </summary>
    public enum CheckpointType
    {
        /// <summary>
        /// Take separate snapshot of in-memory portion of log (default)
        /// </summary>
        Snapshot,

        /// <summary>
        /// Flush current log (move read-only to tail)
        /// (enables incremental checkpointing, but log grows faster)
        /// </summary>
        FoldOver
    }

    /// <summary>
    /// Checkpoint-related settings
    /// </summary>
    public class CheckpointSettings
    {
        /// <summary>
        /// Checkpoint manager
        /// </summary>
        public ICheckpointManager CheckpointManager = null;

        /// <summary>
        /// Type of checkpoint
        /// </summary>
        public CheckpointType CheckPointType = CheckpointType.Snapshot;

        /// <summary>
        /// Use specified directory for storing and retrieving checkpoints
        /// This is a shortcut to providing the following:
        ///   CheckpointSettings.CheckpointManager = new LocalCheckpointManager(CheckpointDir)
        /// </summary>
        public string CheckpointDir = null;
    }
}
