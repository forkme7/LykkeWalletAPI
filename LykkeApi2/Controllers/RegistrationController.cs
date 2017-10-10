using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Registration;
using Lykke.Service.Registration.Models;
using LykkeApi2.Credentials;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
﻿using Core.Settings;
using LykkeApi2.Models.ClientAccountModels;
using LykkeApi2.Models.ResponceModels;
using LykkeApi2.Models.ValidationModels;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    [ValidateModel]
    public class RegistrationController : Controller
    {
        private readonly IClientAccountClient _clientAccountClient;
        private readonly ILykkeRegistrationClient _lykkeRegistrationClient;
        private readonly ClientAccountLogic _clientAccountLogic;

        public RegistrationController(
            ClientAccountLogic clientAccountLogic,
            IClientAccountClient clientAccountClient,
            ILykkeRegistrationClient lykkeRegistrationClient)
        {
            _lykkeRegistrationClient = lykkeRegistrationClient;
            _clientAccountClient = clientAccountClient;
            _clientAccountLogic = clientAccountLogic;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]AccountRegistrationModel model)
        {
            if (await _clientAccountLogic.IsTraderWithEmailExistsForPartnerAsync(model.Email, model.PartnerId))
            {
                ModelState.AddModelError("email", Phrases.ClientWithEmailIsRegistered);
                return BadRequest(new ApiBadRequestResponse(ModelState));
            }

            bool isIosDevice = this.IsIosDevice();

            var registrationModel = new RegistrationModel
            {
                ClientInfo = model.ClientInfo,
                UserAgent = this.GetUserAgent(),
                ContactPhone = model.ContactPhone,
                Email = model.Email,
                FullName = model.FullName,
                Hint = model.Hint,
                PartnerId = model.PartnerId,
                Password = model.Password,
                Ip = this.GetIp(),
                Changer = RecordChanger.Client,
                IosVersion = isIosDevice ? this.GetVersion() : null,
                Referer = HttpContext.Request.Host.Host
            };

            RegistrationResponse result = await _lykkeRegistrationClient.RegisterAsync(registrationModel);

            if (result == null)
                return NotFound(new ApiResponse(HttpStatusCode.InternalServerError, Phrases.TechnicalProblems));

            var resultPhone = await _clientAccountClient.InsertIndexedByPhoneAsync(
                                                        new IndexByPhoneRequestModel()
                                                        {
                                                            ClientId = result.Account.Id,
                                                            PhoneNumber = result.PersonalData.Phone,
                                                            PreviousPhoneNumber = null
                                                        });

            return Ok(new AccountsRegistrationResponseModel
            {
                Token = result.Token,
                NotificationsId = result.NotificationsId,
                PersonalData = new ApiPersonalDataModel
                {
                    FullName = result.PersonalData.FullName,
                    Email = result.PersonalData.Email,
                    Phone = result.PersonalData.Phone
                },
                CanCashInViaBankCard = result.CanCashInViaBankCard,
                SwiftDepositEnabled = result.SwiftDepositEnabled
            });
        }
    }
}