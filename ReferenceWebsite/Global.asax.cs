﻿using Umbraco.Web;
using Umbraco.Web.Mvc;
using UmbracoVault;
using UmbracoVault.Controllers;

namespace ReferenceWebsite
{
    public class Global : UmbracoApplication
    {
        protected override void OnApplicationStarting(object sender, System.EventArgs e)
        {
            DefaultRenderMvcControllerResolver
                .Current
                .SetDefaultControllerType(typeof(VaultDefaultGenericController));

            Vault.RegisterViewModelNamespace("ReferenceWebsite.Models", "ReferenceWebsite");
        }
    }
}