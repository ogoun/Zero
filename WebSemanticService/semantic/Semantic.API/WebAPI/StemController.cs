using Semantic.API.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using ZeroLevel.Services.Semantic;
using ZeroLevel.Services.Web;
using ZeroLevel.WebAPI;

namespace Semantic.API.WebAPI
{
    [RequestFirewall]
    public sealed class StemController : BaseController
    {
        private readonly static ILexProvider _provider = LexProviderFactory.CreateStemProvider(Languages.Russian);

        #region GET
        [HttpGet]
        [Route("api/stem")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage GetStems(HttpRequestMessage request)
        {
            try
            {
                var keys = UrlUtility.ParseQueryString(request.RequestUri.Query);
                var text = keys.ContainsKey("text") ? keys["text"] : string.Empty;
                return request.CreateSelfDestroyingResponse(
                    _provider.ExtractLexTokens(text));
            }
            catch (Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpGet]
        [Route("api/stem/unique")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage GetUniqueStems(HttpRequestMessage request)
        {
            try
            {
                var keys = UrlUtility.ParseQueryString(request.RequestUri.Query);
                var text = keys.ContainsKey("text") ? keys["text"] : string.Empty;
                return request.CreateSelfDestroyingResponse(
                    _provider.ExtractUniqueLexTokens(text));
            }
            catch (Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpGet]
        [Route("api/stem/clean")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage GetCleanStems(HttpRequestMessage request)
        {
            try
            {
                var keys = UrlUtility.ParseQueryString(request.RequestUri.Query);
                var text = keys.ContainsKey("text") ? keys["text"] : string.Empty;
                return request.CreateSelfDestroyingResponse(
                    _provider.ExtractUniqueLexTokensWithoutStopWords(text));
            }
            catch (Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpGet]
        [Route("api/stem/occurences/words")]
        [ResponseType(typeof(IDictionary<string, IEnumerable<LexToken>>))]
        public HttpResponseMessage GetStemsByWordsOccerencesInText(HttpRequestMessage request, [FromUri]string text, [FromUri]string[] words)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(
                    _provider.SearchLexTokensByWords(text, words));
            }
            catch (Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpGet]
        [Route("api/stem/occurences/phrases")]
        [ResponseType(typeof(IDictionary<string, IEnumerable<LexToken[]>>))]
        public HttpResponseMessage GetStemsByPhrasesOccerencesInText(HttpRequestMessage request, [FromUri]string text, [FromUri]string[] words)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(
                    _provider.SearchLexTokensByPhrases(text, words));
            }
            catch (Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }
        #endregion

        #region POST
        [HttpPost]
        [Route("api/stem")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage PostStems(HttpRequestMessage request, [FromBody]string text)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(_provider.ExtractLexTokens(text));
            }
            catch (Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpPost]
        [Route("api/stem/unique")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage PostUniqueStems(HttpRequestMessage request, [FromBody]string text)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(_provider.ExtractUniqueLexTokens(text));
            }
            catch (Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpPost]
        [Route("api/stem/clean")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage PostCleanStems(HttpRequestMessage request, [FromBody]string text)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(_provider.ExtractUniqueLexTokensWithoutStopWords(text));
            }
            catch (Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpPost]
        [Route("api/stem/occurences/words")]
        [ResponseType(typeof(IDictionary<string, IEnumerable<LexToken>>))]
        public HttpResponseMessage PostStemsByWordsOccerencesInText(HttpRequestMessage request, [FromBody]WordsSearchRequest query)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(
                    _provider.SearchLexTokensByWords(query.Text, query.Words));
            }
            catch (Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpPost]
        [Route("api/stem/occurences/phrases")]
        [ResponseType(typeof(IDictionary<string, IEnumerable<LexToken[]>>))]
        public HttpResponseMessage PostStemsByPhrasesOccerencesInText(HttpRequestMessage request, [FromBody]WordsSearchRequest query)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(
                    _provider.SearchLexTokensByPhrases(query.Text, query.Words));
            }
            catch (Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }
        #endregion
    }
}
