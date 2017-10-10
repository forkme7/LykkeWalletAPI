﻿using Lykke.Service.Assets.Client.Custom;
using LykkeApi2.Models;
using LykkeApi2.Models.ResponceModels;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    public class AssetsController : Controller
    {
        private readonly ICachedAssetsService _assetsService;

        public AssetsController(ICachedAssetsService assetsService)
        {
            _assetsService = assetsService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var assets = (await _assetsService.GetAllAssetsAsync()).Where(x => !x.IsDisabled);
            return Ok(GetBaseAssetsRespModel.Create(assets.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var asset = await _assetsService.TryGetAssetAsync(id);
            if (asset == null)
            {
                ModelState.AddModelError("id", $"Asset {id} does not exist");
                return NotFound(new ApiBadRequestResponse(ModelState));
            }
            return Ok(GetClientBaseAssetRespModel.Create(asset.ConvertToApiModel()));
        }

        [HttpGet("{assetId}/attributes")]
        public async Task<IActionResult> GetAssetAttributes(string assetId)
        {
            var keyValues = await _assetsService.GetAssetAttributesAsync(assetId);
            return Ok(keyValues.ConvertToApiModel());
        }

        [HttpGet("{assetId}/attributes/{key}")]
        public async Task<IActionResult> GetAssetAttributeByKey(string assetId, string key)
        {
            var keyValues = await _assetsService.GetAssetAttributeByKeyAsync(assetId, key);
            return Ok(keyValues.ConvertToApiModel().Attrbuttes.FirstOrDefault() ?? new KeyValue());
        }

        [HttpPost("description")]
        public async Task<IActionResult> GetAssetDescriptions([FromBody]GetAssetDescriptionsRequestModel request)
        {
            var res = await _assetsService.GetAssetDescriptionsAsync(new Lykke.Service.Assets.Client.Models.GetAssetDescriptionsRequestModel { Ids = request.Ids });
            return Ok(AssetDescriptionsResponseModel.Create(res.Select(s => s.ConvertToApiModel()).ToList()));
        }

        [HttpGet("{assetId}/description")]
        public async Task<IActionResult> GetAssetDescription(string assetId)
        {
            var res = await _assetsService.GetAssetDescriptionsAsync(new Lykke.Service.Assets.Client.Models.GetAssetDescriptionsRequestModel { Ids = new List<string> { assetId } });
            return Ok(AssetDescriptionsResponseModel.Create(res.Select(s => s.ConvertToApiModel()).ToList()));
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetAssetCategories()
        {
            var res = await _assetsService.GetAssetCategoriesAsync();
            return Ok(GetAssetCategoriesResponseModel.Create(res.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        [HttpGet("{assetId}/categories")]
        public async Task<IActionResult> GetAssetCategory(string assetId)
        {
            var res = await _assetsService.TryGetAssetCategoryAsync(assetId);

            if (res.errorResponse != null)
            {
                return NotFound(new ApiResponse(HttpStatusCode.NotFound, $"Error while retrieving asset category for asset {assetId}. {res.errorResponse.ErrorMessages}"));
            }

            return Ok(GetAssetCategoriesResponseModel.Create(new ApiAssetCategoryModel[] { res.ConvertToApiModel() }));
        }

        [HttpGet("extended")]
        public async Task<IActionResult> GetAssetsExtended()
        {
            var res = await _assetsService.GetAssetsExtendedAsync();
            var assetsExtended = res.Assets.Select(s => s.ConvertTpApiModel()).ToList();
            return Ok(AssetExtendedResponseModel.Create(assetsExtended));
        }

        [HttpGet("{assetId}/extended")]
        public async Task<IActionResult> GetAssetsExtended(string assetId)
        {
            var res = await _assetsService.GetAssetExtendedByIdAsync(assetId);
            var assetsExtended = res.Assets.Select(s => s.ConvertTpApiModel()).ToList();
            return Ok(AssetExtendedResponseModel.Create(assetsExtended));
        }

    }
}