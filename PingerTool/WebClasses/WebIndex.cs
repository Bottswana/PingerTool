using System;
using Nancy;
using PingerTool.Classes;
using System.ComponentModel.Composition;

namespace PingerTool.WebClasses
{
    [Export(typeof(NancyModule))]
	public class WebIndex : WebModule
	{
        #region Initialisation
		public WebIndex()
		{   
            Get["/"] = IndexFunction;
        }
        #endregion Initialisation

        #region Webserver Routes
        /// <summary>
        /// Index Webpage
        /// </summary>
        private dynamic IndexFunction( dynamic parameters )
        {
            if( ! CheckWhitelisted(Request?.UserHostAddress) ) return ThrowUnauthorized();
            return View["Index.html"];
        }
        #endregion Webserver Routes
	}
}