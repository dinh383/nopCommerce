﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Web.Areas.Admin.Extensions;
using Nop.Web.Areas.Admin.Models.Payments;
using Nop.Web.Framework.Extensions;

namespace Nop.Web.Areas.Admin.Factories
{
    /// <summary>
    /// Represents the payment method model factory implementation
    /// </summary>
    public partial class PaymentModelFactory : IPaymentModelFactory
    {
        #region Fields

        private readonly ICountryService _countryService;
        private readonly ILocalizationService _localizationService;
        private readonly IPaymentService _paymentService;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly PaymentSettings _paymentSettings;

        #endregion

        #region Ctor

        public PaymentModelFactory(ICountryService countryService,
            ILocalizationService localizationService,
            IPaymentService paymentService,
            IWebHelper webHelper,
            IWorkContext workContext,
            PaymentSettings paymentSettings)
        {
            this._countryService = countryService;
            this._localizationService = localizationService;
            this._paymentService = paymentService;
            this._webHelper = webHelper;
            this._workContext = workContext;
            this._paymentSettings = paymentSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare payment methods model
        /// </summary>
        /// <param name="methodsModel">Payment methods model</param>
        /// <returns>Payment methods model</returns>
        public virtual PaymentMethodsModel PreparePaymentMethodsModel(PaymentMethodsModel methodsModel)
        {
            if (methodsModel == null)
                throw new ArgumentNullException(nameof(methodsModel));

            //prepare nested search models
            PreparePaymentMethodSearchModel(methodsModel.PaymentsMethod);
            PreparePaymentMethodRestrictionModel(methodsModel.PaymentMethodRestriction);

            return methodsModel;
        }

        /// <summary>
        /// Prepare payment method search model
        /// </summary>
        /// <param name="searchModel">Payment method search model</param>
        /// <returns>Payment method search model</returns>
        public virtual PaymentMethodSearchModel PreparePaymentMethodSearchModel(PaymentMethodSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged payment method list model
        /// </summary>
        /// <param name="searchModel">Payment method search model</param>
        /// <returns>Payment method list model</returns>
        public virtual PaymentMethodListModel PreparePaymentMethodListModel(PaymentMethodSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get payment methods
            var paymentMethods = _paymentService.LoadAllPaymentMethods();

            //prepare grid model
            var model = new PaymentMethodListModel
            {
                Data = paymentMethods.PaginationByRequestModel(searchModel).Select(method =>
                {
                    //fill in model values from the entity
                    var paymentMethodModel = method.ToModel();

                    //fill in additional values (not existing in the entity)
                    paymentMethodModel.IsActive = method.IsPaymentMethodActive(_paymentSettings);
                    paymentMethodModel.ConfigurationUrl = method.GetConfigurationPageUrl();
                    paymentMethodModel.LogoUrl = method.PluginDescriptor.GetLogoUrl(_webHelper);
                    paymentMethodModel.RecurringPaymentType = method.RecurringPaymentType.GetLocalizedEnum(_localizationService, _workContext);

                    return paymentMethodModel;
                }),
                Total = paymentMethods.Count
            };

            return model;
        }

        /// <summary>
        /// Prepare payment method restriction model
        /// </summary>
        /// <param name="model">Payment method restriction model</param>
        /// <returns>Payment method restriction model</returns>
        public virtual PaymentMethodRestrictionModel PreparePaymentMethodRestrictionModel(PaymentMethodRestrictionModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var countries = _countryService.GetAllCountries(showHidden: true);
            model.AvailableCountries = countries.Select(country => country.ToModel()).ToList();

            foreach (var method in _paymentService.LoadAllPaymentMethods())
            {
                model.AvailablePaymentMethods.Add(method.ToModel());

                var restrictedCountries = _paymentService.GetRestictedCountryIds(method);
                foreach (var country in countries)
                {
                    if (!model.Resticted.ContainsKey(method.PluginDescriptor.SystemName))
                        model.Resticted[method.PluginDescriptor.SystemName] = new Dictionary<int, bool>();

                    model.Resticted[method.PluginDescriptor.SystemName][country.Id] = restrictedCountries.Contains(country.Id);
                }
            }

            return model;
        }

        #endregion
    }
}