﻿using Common.Log;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.CandlesHistory.Client.Custom;
using Lykke.Service.CandlesHistory.Client.Models;
using LykkeApi2.Models;
using LykkeApi2.Models.ValidationModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Candles;
using Core.Enumerators;
using LkeServices.Candles;

namespace LykkeApi2.Controllers
{
    [Route("api/candlesHistory")]
    [ValidateModel]
    public class CandlesHistoryController : Controller
    {
        private readonly ICandlesHistoryServiceProvider _candlesServiceProvider;
        private readonly ILog _log;

        public CandlesHistoryController(ICandlesHistoryServiceProvider candlesServiceProvider, ILog log)
        {
            _candlesServiceProvider = candlesServiceProvider;
            _log = log;
        }

        /// <summary>
        /// AssetPairs candles history
        /// </summary>
        /// <param name="assetPairId">Asset pair ID</param>
        /// <param name="priceType">Price type</param>
        /// <param name="timeInterval">Time interval</param>
        /// <param name="fromMoment">From moment in ISO 8601 (inclusive)</param>
        /// <param name="toMoment">To moment in ISO 8601 (exclusive)</param>
        [HttpGet("{type}/{assetPairId}/{priceType}/{timeInterval}/{fromMoment:datetime}/{toMoment:datetime}")]
        public async Task<IActionResult> Get([FromRoute]CandleSticksRequestModel request)
        {
            try
            {
                var candleHistoryService = _candlesServiceProvider.Get(request.Type);
                
                var candles = await candleHistoryService.GetCandlesHistoryAsync(request.AssetPairId, (CandlePriceType)Enum.Parse(typeof(CandlePriceType), request.PriceType.ToString()), (CandleTimeInterval)Enum.Parse(typeof(CandleTimeInterval), request.TimeInterval.ToString()), request.FromMoment, request.ToMoment);
                
                return Ok(candles.ToResponseModel());
            }
            catch (ErrorResponseException ex)
            {
                var errors = ex.Error.ErrorMessages.Values.SelectMany(s => s.Select(ss => ss));
                return NotFound($"{string.Join(',', errors)}");
            }
        }
    }
}
