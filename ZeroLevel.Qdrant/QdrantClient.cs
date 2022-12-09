using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ZeroLevel.Models;
using ZeroLevel.Qdrant.Models.Filters;
using ZeroLevel.Qdrant.Models.Requests;
using ZeroLevel.Qdrant.Models.Responces;

namespace ZeroLevel.Qdrant
{
    /*
     https://qdrant.github.io/qdrant/redoc/index.html#operation/search_points
     https://qdrant.tech/documentation/search/
     */
    /// <summary>
    /// Client for Qdrant API
    /// </summary>
    public class QdrantClient
    {
        private const int DEFAULT_OPERATION_TIMEOUT_S = 30;
        private HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
            };
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            return new HttpClient(handler)
            {
                BaseAddress = _serverUri,
                Timeout = TimeSpan.FromMinutes(5)
            };
        }
        private readonly Uri _serverUri;
        public QdrantClient(string host = "localhost", int port = 6333)
        {
            _serverUri = new Uri($"{host}:{port}");
        }

        #region API

        #region Collection https://qdrant.github.io/qdrant/redoc/index.html#tag/collections
        /// <summary>
        /// Create collection
        /// </summary>
        /// <param name="name">Collection name</param>
        /// <param name="distance">Cosine or Dot or Euclid</param>
        /// <param name="vector_size">Count of elements in vectors</param>
        /// <returns></returns>
        public async Task<InvokeResult<OperationResponse>> CreateCollection(string name, string distance, int vector_size, bool? on_disk_payload)
        {
            try
            {
                var collection = new CreateCollectionReqeust(distance, vector_size, on_disk_payload);
                var json = JsonConvert.SerializeObject(collection);
                var data = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"/collections/{name}";

                var response = await _request<OperationResponse>(url, new HttpMethod("PUT"), data);
                return InvokeResult.Succeeding<OperationResponse>(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[QdrantClient.CreateCollection] Name: {name}. Distance: {distance}. Vector size: {vector_size}");
                return InvokeResult.Fault<OperationResponse>($"[QdrantClient.CreateCollection] Name: {name}\r\n{ex.ToString()}");
            }
        }
        /// <summary>
        /// Delete collection by name
        /// </summary>
        /// <param name="name">Collection name</param>
        public async Task<InvokeResult<OperationResponse>> DeleteCollection(string name, int timeout = DEFAULT_OPERATION_TIMEOUT_S)
        {
            try
            {
                var url = $"/collections/{name}?timeout={timeout}";
                var response = await _request<OperationResponse>(url, new HttpMethod("DELETE"), null);
                return InvokeResult.Succeeding<OperationResponse>(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[QdrantClient.DeleteCollection] Name: {name}.");
                return InvokeResult.Fault<OperationResponse>($"[QdrantClient.DeleteCollection] Name: {name}\r\n{ex.ToString()}");
            }
        }
        #endregion

        #region Indexes https://qdrant.tech/documentation/indexing/
        /// <summary>
        /// For indexing, it is recommended to choose the field that limits the search result the most. As a rule, the more different values a payload value has, the more efficient the index will be used. You should not create an index for Boolean fields and fields with only a few possible values.
        /// </summary>
        public async Task<InvokeResult<CreateIndexResponse>> CreateIndex(string collection_name, string field_name, IndexFieldType field_type)
        {
            try
            {
                var index = new CreateIndexRequest(field_name, field_type);
                var json = JsonConvert.SerializeObject(index);
                var data = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"/collections/{collection_name}/index";

                var response = await _request<CreateIndexResponse>(url, new HttpMethod("PUT"), data);
                return InvokeResult.Succeeding<CreateIndexResponse>(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[QdrantClient.CreateIndex] Collection name: {collection_name}. Field name: {field_name}");
                return InvokeResult.Fault<CreateIndexResponse>($"[QdrantClient.CreateIndex]  Collection name: {collection_name}. Field name: {field_name}\r\n{ex.ToString()}");
            }
        }
        #endregion

        #region Search https://qdrant.tech/documentation/search/
        /// <summary>
        /// Searching for the nearest vectors
        /// </summary>
        public async Task<InvokeResult<SearchResponse>> Search(string collection_name, double[] vector, uint top, Filter filter = null)
        {
            try
            {
                var search = new SearchRequest { FloatVector = vector, Top = top, Filter = filter };
                var json = search.ToJson();
                var data = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"/collections/{collection_name}/points/search";

                var response = await _request<SearchResponse>(url, new HttpMethod("POST"), data);
                return InvokeResult.Succeeding<SearchResponse>(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[QdrantClient.Search] Collection name: {collection_name}.");
                return InvokeResult.Fault<SearchResponse>($"[QdrantClient.Search]  Collection name: {collection_name}.\r\n{ex.ToString()}");
            }
        }
        /// <summary>
        /// Searching for the nearest vectors
        /// </summary>
        public async Task<InvokeResult<SearchResponse>> Search(string collection_name, long[] vector, uint top, Filter filter = null)
        {
            try
            {
                var search = new SearchRequest { IntegerVector = vector, Top = top, Filter = filter };
                var json = search.ToJson();
                var data = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"/collections/{collection_name}/points/search";

                var response = await _request<SearchResponse>(url, new HttpMethod("POST"), data);
                return InvokeResult.Succeeding<SearchResponse>(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[QdrantClient.Search] Collection name: {collection_name}.");
                return InvokeResult.Fault<SearchResponse>($"[QdrantClient.Search]  Collection name: {collection_name}.\r\n{ex.ToString()}");
            }
        }
        #endregion

        #region Points https://qdrant.tech/documentation/points/
        /// <summary>
        /// There is a method for retrieving points by their ids.
        /// </summary>
        public async Task<InvokeResult<PointResponse>> GetPoint(string collection_name, long id)
        {
            try
            {
                string url = $"/collections/{collection_name}/points/{id}";
                var response = await _request<PointResponse>(url, new HttpMethod("GET"), null);
                return InvokeResult.Succeeding<PointResponse>(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[QdrantClient.Points] Collection name: {collection_name}.");
                return InvokeResult.Fault<PointResponse>($"[QdrantClient.GetPoint]  Collection name: {collection_name}. Point ID: {id}\r\n{ex.ToString()}");
            }
        }

        /// <summary>
        /// There is a method for retrieving points by their ids.
        /// </summary>
        public async Task<InvokeResult<PointsResponse>> GetPoints(string collection_name, long[] ids)
        {
            try
            {
                var points = new PointsRequest { ids = ids };
                var json = JsonConvert.SerializeObject(points);
                var data = new StringContent(json, Encoding.UTF8, "application/json");
                string url = $"/collections/{collection_name}/points";
                var response = await _request<PointsResponse>(url, new HttpMethod("POST"), data);
                return InvokeResult.Succeeding<PointsResponse>(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[QdrantClient.Points] Collection name: {collection_name}.");
                return InvokeResult.Fault<PointsResponse>($"[QdrantClient.GetPoints]  Collection name: {collection_name}.\r\n{ex.ToString()}");
            }
        }

        /// <summary>
        /// There is a method for retrieving points by their ids.
        /// </summary>
        public async Task<InvokeResult<ScrollResponse>> Scroll(string collection_name, Filter filter, long limit, long offset = 0, bool with_vector = true, bool with_payload = true)
        {
            try
            {
                var scroll = new ScrollRequest { Filter = filter, Limit = limit, Offset = offset, WithPayload = with_payload, WithVector = with_vector };
                var json = scroll.ToJson();
                var data = new StringContent(json, Encoding.UTF8, "application/json");
                string url = url = $"/collections/{collection_name}/points/scroll";

                var response = await _request<ScrollResponse>(url, new HttpMethod("POST"), data);
                return InvokeResult.Succeeding<ScrollResponse>(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[QdrantClient.Scroll] Collection name: {collection_name}.");
                return InvokeResult.Fault<ScrollResponse>($"[QdrantClient.Scroll]  Collection name: {collection_name}.\r\n{ex.ToString()}");
            }
        }


        /// <summary>
        /// Record-oriented of creating batches
        /// </summary>
        public async Task<InvokeResult<PointsOperationResponse>> UpsertPoints<T>(string collection_name, PointsUpsertRequest<T> points)
        {
            try
            {
                var json = points.ToJSON();
                var data = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"/collections/{collection_name}/points";
                var response = await _request<PointsOperationResponse>(url, new HttpMethod("PUT"), data);
                return InvokeResult.Succeeding<PointsOperationResponse>(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[QdrantClient.UpsertPoints] Collection name: {collection_name}.");
                return InvokeResult.Fault<PointsOperationResponse>($"[QdrantClient.UpsertPoints]  Collection name: {collection_name}.\r\n{ex.ToString()}");
            }
        }
        /// <summary>
        /// Delete points by their ids.
        /// </summary>
        public async Task<InvokeResult<PointsOperationResponse>> DeletePoints(string collection_name, long[] ids)
        {
            try
            {
                var points = new DeletePointsRequest { delete_points = new DeletePoints { ids = ids } };
                var json = JsonConvert.SerializeObject(points);
                var data = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"/collections/{collection_name}";

                var response = await _request<PointsOperationResponse>(url, new HttpMethod("POST"), data);
                return InvokeResult.Succeeding<PointsOperationResponse>(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[QdrantClient.DeleteCollection] Name: {collection_name}.");
                return InvokeResult.Fault<PointsOperationResponse>($"[QdrantClient.DeleteCollection] Name: {collection_name}\r\n{ex.ToString()}");
            }
        }
        #endregion

        #endregion

        #region Private
        private async Task<T> _request<T>(string url, HttpMethod method, HttpContent content = null)
        {
            var json = await _request(url, method, content);
            return JsonConvert.DeserializeObject<T>(json);
        }

        private async Task<string> _request(string url, HttpMethod method, HttpContent content = null)
        {
            var fullUrl = new Uri(_serverUri, url);
            var message = new HttpRequestMessage(method, fullUrl) { Content = content };
            using (var client = CreateClient())
            {
                var response = await client.SendAsync(message);
                var result = await response.Content.ReadAsStringAsync();
                var jsonPrint = result?.Length >= 5000 ? "<BIG CONTENT>" : result;
                if (response.IsSuccessStatusCode == false)
                {
                    throw new Exception($"Not SuccessStatusCode {method} {fullUrl}. Status: {response.StatusCode} {response.ReasonPhrase}. Content: {jsonPrint}");
                }
                return result;
            }
        }
        #endregion
    }
}
