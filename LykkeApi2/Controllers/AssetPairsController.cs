﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.MarketProfileService.Client;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using LykkeApi2.Models.AssetPairRates;
using LykkeApi2.Models.AssetPairsModels;
using LykkeApi2.Models.ValidationModels;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    [ValidateModel]
    public class AssetPairsController : Controller
    {
        private readonly CachedDataDictionary<string, AssetPair> _assetPairs;
        private readonly IAssetsService _assetsService;
        private readonly ILog _log;
        private readonly ILykkeMarketProfileServiceAPI _marketProfileService;
        private readonly IRequestContext _requestContext;

        public AssetPairsController(
            CachedDataDictionary<string, AssetPair> assetPairs,
            IAssetsService assetsService,
            ILykkeMarketProfileServiceAPI marketProfile,
            ILog log,
            IRequestContext requestContext)
        {
            _assetPairs = assetPairs;
            _assetsService = assetsService;
            _marketProfileService = marketProfile;
            _log = log;
            _requestContext = requestContext;
        }

        /// <summary>
        ///     Get asset pairs.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(Models.AssetPairsModels.AssetPairResponseModel), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            var assetPairs = (await _assetPairs.Values()).Where(s => !s.IsDisabled);
            return Ok(Models.AssetPairsModels.AssetPairResponseModel.Create(assetPairs.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        /// <summary>
        ///     Get asset pair by id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Models.AssetPairsModels.AssetPairResponseModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetAssetPairById(string id)
        {
            var assetPair = (await _assetPairs.Values()).FirstOrDefault(x => x.Id == id);
            if (assetPair == null)
                return NotFound($"AssetPair {id} does not exist");
            return Ok(Models.AssetPairsModels.AssetPairResponseModel.Create(new List<AssetPairModel> {assetPair.ConvertToApiModel()}));
        }

        /// <summary>
        ///     Get asset pair rates.
        /// </summary>
        /// <returns></returns>
        [HttpGet("rates")]
        [ProducesResponseType(typeof(AssetPairRatesResponseModel), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetAssetPairRates()
        {
            var assetPairs = await _assetsService.AssetPairGetAllAsync();
            //var assetPairs = await _assetsService.GetAssetsPairsForClient(new Lykke.Service.Assets.Client.Models.GetAssetPairsForClientRequestModel
            //{
            //    ClientId = _requestContext.ClientId,
            //    IsIosDevice = _requestContext.IsIosDevice,
            //    PartnerId = _requestContext.PartnerId
            //});

            var assetPairsDict = assetPairs.ToDictionary(itm => itm.Id);
            var marketProfile = await _marketProfileService.ApiMarketProfileGetAsync();

            marketProfile = marketProfile.Where(itm => assetPairsDict.ContainsKey(itm.AssetPair)).ToList();
            return Ok(AssetPairRatesResponseModel.Create(marketProfile.Select(m => m.ConvertToApiModel()).ToArray()));
        }

        /// <summary>
        ///     Get asset pair rates by id.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("rates/{assetPairId}")]
        [ProducesResponseType(typeof(AssetPairRatesResponseModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetAssetPairRatesById([FromRoute] AssetPairRequestModel request)
        {
            var asset = (await _assetPairs.Values()).FirstOrDefault(x => x.Id == request.AssetPairId);

            if (asset == null)
                return NotFound($"AssetPair {request.AssetPairId} does not exist");

            var marketProfile = await _marketProfileService.ApiMarketProfileGetAsync();
            var feedData = marketProfile.FirstOrDefault(itm => itm.AssetPair == request.AssetPairId);

            if (feedData == null)
                return NotFound($"No data exist for {request.AssetPairId}");

            return Ok(AssetPairRatesResponseModel.Create(new List<AssetPairRateModel> {feedData.ConvertToApiModel()}));
        }
    }
}