using System;
using Nancy;
using Nancy.TinyIoc;
using Nancy.Security;
using System.Windows;
using Nancy.ViewEngines;
using Nancy.Conventions;
using Nancy.Hosting.Self;
using Nancy.Bootstrapper;
using Nancy.Embedded.Conventions;
using Nancy.Authentication.Basic;
using System.Collections.Generic;

namespace PingerTool.Classes
{
    /*
     * Problems:
     * Save details in project
     * Authentication doesnt work
     * Allowed Subnets not finished
     * Nancy bug where we get an exception if we stop then start. Find workaround and report to their github.
     */

    public class WebServer : IDisposable
    {
        public static string[] AllowedSubnets = {"0.0.0.0/0"}; // TODO: Implement This!
        public static string[] AuthDetails = {"", ""};
        public static string BindAddress = "";

        public static bool AuthEnabled = false;
        public static bool UseEmbedded = false;

        private bool _DisposedValue = false;
        private NancyHost _Server;
        private App _AppRef;

        #region Initialisation
        /// <summary>
        /// Create a new WebServer Instance using Nancy
        /// </summary>
        /// <param name="BindAddress">Address to bind to</param>
        /// <param name="AllowedSubnets">Subnets to allow access from</param>
        /// <param name="Authentication">If authentication should be required or not</param>
        /// <param name="Username">Username for authentication, if enabled</param>
        /// <param name="Password">Password for authentication, if enabled</param>
        public WebServer( string BindAddress, string AllowedSubnets = "0.0.0.0/0", bool Authentication = false, string Username = "", string Password = "" )
        {
            // Validate Parameters
            _AppRef = App.GetApp();
            if( Authentication && ( Username == null || Username.Length < 1 || Password == null || Password.Length < 1 ) )
            {
                _AppRef.Log.Warn("Authentication disabled as Username or Password is not set");
            }
            else if( Authentication )
            {
                AuthDetails = (new List<string>() { Username, Password }).ToArray();
                AuthEnabled = true;
            }

            if( AllowedSubnets == null || AllowedSubnets.Length < 1 )
            {
                _AppRef.Log.Warn("Subnet restriction disabled as it is not set to a valid subnet");
            }
            else
            {
                WebServer.AllowedSubnets = AllowedSubnets.Split(',');
            }

            // Start Webserver
            try
            {
                var Config = new HostConfiguration()
                {
                    UrlReservations = new UrlReservations()
                    {
                        CreateAutomatically = true
                    }
                };

                WebServer.BindAddress = BindAddress;
                if( BindAddress.ToString().Contains("0.0.0.0") )
                {
                    // Rewrite 0.0.0.0 wildcard to bind to all IPs via Nancys RewriteLocalhost feature
                    Config.RewriteLocalhost = true;
                    _Server = new NancyHost(Config, new Uri($"http://{BindAddress.Replace("0.0.0.0", "localhost")}"));
                }
                else
                {
                    // Explicit address bind
                    _Server = new NancyHost(Config, new Uri($"http://{BindAddress}"));
                }
                
                // Start Listening
                _Server.Start();
            }
            catch( Exception Ex )
            {
                MessageBox.Show($"The webserver could not be started. Error:\n{Ex.Message}", "Webserver Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                _AppRef.Log.Error(Ex, "Webserver start error");
            }
            finally
            {
                _AppRef.Log.Info("Webserver started successfully on {0}", BindAddress);
            }
        }

        protected virtual void Dispose( bool disposing )
        {
            if( !_DisposedValue )
            {
                if( disposing )
                {
                    // Dispose Managed Objects
                    if( _Server != null ) _Server.Dispose();
                    _Server = null;
                }

                _DisposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion Initialisation

        #region Basic Authentication
        public class UserValidator : IUserValidator
        {
            public class UserIdentity : IUserIdentity
            {
                public string UserName { get; set; }
                public IEnumerable<string> Claims { get; set; }
            }

            public IUserIdentity Validate(string username, string password)
            {
                if( username.Equals(AuthDetails[0]) && password.Equals(AuthDetails[1]) )
                {
                    return new UserIdentity()
                    {
                        UserName = username,
                        Claims = null
                    };
                }

                // Failed Login
                return null;
            }
        }
        #endregion Basic Authentication
    }

    public class WebCoreBootstrapper : DefaultNancyBootstrapper
    {
		public static string[] StaticDirectories = { "Content", "Scripts", "Fonts" };

        #region Custom Bootstrap
		/// <summary>
		/// Configure view load from Embedded Resources
		/// </summary>
		protected override void ConfigureApplicationContainer(TinyIoCContainer container)
		{
			base.ConfigureApplicationContainer(container);
			ResourceViewLocationProvider.RootNamespaces.Add(GetType().Assembly, "PingerTool.Views");
		}

		/// <summary>
		/// Configure static resource load from Embedded Resources
		/// </summary>
		protected override void ConfigureConventions(NancyConventions nancyConventions)
		{
			base.ConfigureConventions(nancyConventions);
            if( WebServer.UseEmbedded )
			{
				// Use embedded static resources
				foreach( var Dir in StaticDirectories )
				{
					nancyConventions.StaticContentsConventions.Add(EmbeddedStaticContentConventionBuilder.AddDirectory(Dir, GetType().Assembly, Dir));
				}
			}
			else
			{
				// Use static resources on filesystem
				foreach( var Dir in StaticDirectories )
				{
					nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory(Dir));
				}
			}
		}

		/// <summary>
		/// Configure authentication mode
		/// </summary>
		protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
		{
			base.ApplicationStartup(container, pipelines);
            if( WebServer.AuthEnabled )
            {
			    var Authentication = new BasicAuthenticationConfiguration(container.Resolve<IUserValidator>(), "PingerTool Login");
			    pipelines.EnableBasicAuthentication(Authentication);
            }
		}
        #endregion Custom Bootstrap

		#region Override for Embedded Views
		protected override NancyInternalConfiguration InternalConfiguration
		{
			get { return NancyInternalConfiguration.WithOverrides(OnConfigurationBuilder); }
		}

		void OnConfigurationBuilder(NancyInternalConfiguration x)
		{
			if( WebServer.UseEmbedded )
			{
				x.ViewLocationProvider = typeof(ResourceViewLocationProvider);
			}
		}
		#endregion Override for Embedded Views
    }
}