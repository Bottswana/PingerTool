using System;
using Nancy;
using Nancy.TinyIoc;
using Nancy.Security;
using System.Windows;
using Nancy.Conventions;
using Nancy.Hosting.Self;
using Nancy.Bootstrapper;
using PingerTool.Windows;
using LukeSkywalker.IPNetwork;
using Nancy.Authentication.Basic;
using System.Collections.Generic;

/*
 * Note: Using compiler statements to use embedded views when compiled with release build
 * RELEASE BUILDS WILL REQUIRE THE HTML VIEWS BE EMBEDDED AS RESOURCES
 */

#if !DEBUG
using Nancy.ViewEngines;
using Nancy.Embedded.Conventions;
#endif

namespace PingerTool.Classes
{
    public class WebServer : IDisposable
    {
        public readonly string[] AllowedSubnets = {"127.0.0.0/8"};
        public readonly string[] AuthDetails = {"", ""};
        public readonly string BindAddress = "";
        public readonly bool AuthEnabled = false;

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
                this.AllowedSubnets = AllowedSubnets.Split(',');
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

                this.BindAddress = BindAddress;
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

            public IUserIdentity Validate( string username, string password )
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    var MWindow = (MainWindow)App.GetApp()?.MainWindow;
                    if( MWindow != null && MWindow.Server != null )
                    {
                        if( username.Equals(MWindow.Server.AuthDetails[0]) && MWindow.Server.AuthDetails[1].Equals(Helpers.SHA256Hash(password)) )
                        {
                            return new UserIdentity()
                            {
                                UserName = username,
                                Claims = null
                            };
                        }
                    }

                    // Failed Login
                    return null;
                });
            }
        }
        #endregion Basic Authentication
    }

    public class WebCoreBootstrapper : DefaultNancyBootstrapper
    {
        public static string[] StaticDirectories = { "Content", "Scripts", "Fonts" };

        #region Custom Bootstrap
        /// <summary>
        /// Configure static resource load from Embedded Resources
        /// </summary>
        protected override void ConfigureConventions( NancyConventions nancyConventions )
        {
            base.ConfigureConventions(nancyConventions);

            var MWindow = (MainWindow)App.GetApp()?.MainWindow;
            if( MWindow != null && MWindow.Server != null )
            {
                #if !DEBUG
                // Use embedded static resources
                foreach( var Dir in StaticDirectories )
                {
                    nancyConventions.StaticContentsConventions.Add(EmbeddedStaticContentConventionBuilder.AddDirectory(Dir, GetType().Assembly, Dir));
                }
                #else
                // Use static resources on filesystem
                foreach( var Dir in StaticDirectories )
                {
                    nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory(Dir));
                }
                #endif
            }
        }

        /// <summary>
        /// Configure authentication mode
        /// </summary>
        protected override void ApplicationStartup( TinyIoCContainer container, IPipelines pipelines )
        {
            base.ApplicationStartup(container, pipelines);
            var Authentication = new BasicAuthenticationConfiguration(container.Resolve<IUserValidator>(), "PingerTool Login");
            pipelines.EnableBasicAuthentication(Authentication);
        }
        #endregion Custom Bootstrap

        #region Override for Embedded Views
        protected override NancyInternalConfiguration InternalConfiguration
        {
            get { return NancyInternalConfiguration.WithOverrides(OnConfigurationBuilder); }
        }

        void OnConfigurationBuilder( NancyInternalConfiguration x )
        {
            #if !DEBUG
            var MWindow = (MainWindow)App.GetApp()?.MainWindow;
            if( MWindow != null && MWindow.Server != null )
            {
                x.ViewLocationProvider = typeof(ResourceViewLocationProvider);
            }
            #endif
        }
        #endregion Override for Embedded Views
    }

    public class WebModule : NancyModule
    {
        public MainWindow _Window;

        #region Custom Module Base
        public WebModule() : base()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _Window = (MainWindow)App.GetApp()?.MainWindow;
                if( _Window != null && _Window.Server != null && _Window.Server.AuthEnabled )
                {
                    this.RequiresAuthentication();
                }
            });
        }

        /// <summary>
        /// Check if a client IP is allowed to view the page
        /// </summary>
        /// <param name="Client">IP Address of client</param>
        /// <returns>True if allowed, false otherwise</returns>
        public bool CheckWhitelisted(string Client)
        {
            if( Client == null ) return false;
            foreach( var Subnet in _Window.Server.AllowedSubnets )
            {
                if( Subnet.Equals("0.0.0.0/0") )
                {
                    // All Subnets
                    return true;
                }
                else
                {
                    // Validate Subnet
                    var SubnetInstance = IPNetwork.Parse(Subnet);
                    if( SubnetInstance.ContainsAddress(Client) ) return true;
                }
            }

            // Not Authorized
            return false;
        }

        /// <summary>
        /// Returns an Unauthorized response to send to a client
        /// </summary>
        /// <returns>Response class</returns>
        public Response ThrowUnauthorized()
        {
            // Return unauthorized response
            return new Response()
            {
                StatusCode = HttpStatusCode.Forbidden,
                Contents = s =>
                {
                    using( var Writer = new System.IO.StreamWriter(s) )
                    {
                        Writer.WriteLine("HTTP 403 Unauthorized.<br />Your IP is not approved to access this facility");
                    }
                }
            };
        }
        #endregion Custom Module Base
    }
}