using System;
using Nancy;
using System.IO;
using System.Text;
using Nancy.TinyIoc;
using SuperWebSocket;
using Nancy.Security;
using System.Windows;
using Newtonsoft.Json;
using Nancy.ViewEngines;
using System.Reflection;
using Nancy.Conventions;
using Nancy.Bootstrapper;
using Nancy.Hosting.Self;
using PingerTool.Windows;
using PingerTool.Controls;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using LukeSkywalker.IPNetwork;
using System.Collections.Generic;
using Nancy.Authentication.Basic;
using SuperSocket.SocketBase.Config;

/*
 * Note: Using compiler statements to use embedded views when compiled with release build
 * RELEASE BUILDS WILL REQUIRE THE HTML VIEWS BE EMBEDDED AS RESOURCES
 */

#if !DEBUG
using Nancy.Embedded.Conventions;
#endif

namespace PingerTool.Classes
{
    public class WebServer : IDisposable
    {
        public List<string> AuthorizedWebSocketTokens = new List<string>();
        public readonly string[] AllowedSubnets = { "127.0.0.0/8" };
        public readonly string[] AuthDetails = { "", "" };
        public readonly bool AuthEnabled = false;
        public readonly string BindAddress = "";

        private bool _DisposedValue = false;
        private WebSockServer _WebSock;
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
        public WebServer(string BindAddress, string AllowedSubnets = "0.0.0.0/0", bool Authentication = false, string Username = "", string Password = "")
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

            try
            {
                // Start Webserver
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

                // Start Websocket Server
                var SplitAddress = BindAddress.Split(':');
                var BindPort = ( SplitAddress.Length >= 2 ) ? int.Parse(SplitAddress[1])+1 : 81;

                // Start WebSocket Server
                _WebSock = new WebSockServer(SplitAddress[0], BindPort, this);
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

        protected virtual void Dispose(bool disposing)
        {
            if( !_DisposedValue )
            {
                if( disposing )
                {
                    // Dispose Managed Objects
                    if( _WebSock != null ) _WebSock.Dispose();
                    if( _Server != null ) _Server.Dispose();
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

        #region Public Methods
        public void SendWebsocketMessage(PingControlModel Model, string MessageLine)
        {
            var Message = JsonConvert.SerializeObject(new WebSockServer.SocketMessage()
            {
                IsPaused = (Model.PauseIcon != FontAwesome.WPF.FontAwesomeIcon.Pause),
                Address = Model.Address.ToString(),
                Colour = Model.Colour.ToString(),
                DisplayName = Model.DisplayName,
                LastContact = Model.LastContact,
                Message = MessageLine.Trim(),
                Alerting = Model.Alerting,
            });

            _WebSock?.SendSocketMessage(Message);
        }
        #endregion Public Methods
    }

    public class WebSockServer : IDisposable
    {
        private List<WebSocketSession> _Sessions = new List<WebSocketSession>();
        private bool _DisposedValue = false;
        private WebSocketServer _Server;
        private MainWindow _Window;
        private WebServer _Parent;

        #region Sub Classes
        public class SocketChallenge
        {
            public string Token { get; set; }
        }

        public class SocketMessage
        {
            public string DisplayName {  get; set; }
            public string LastContact { get; set; }
            public string Address { get; set; }
            public string Colour { get; set; }
            public bool IsPaused { get; set; }
            public bool Alerting { get; set; }
            public string Message { get; set; }
        }
        #endregion Sub Classes

        #region Initialisation
        public WebSockServer(string BindAddress, int Port, WebServer Parent)
        {
            _Parent = Parent;
            Application.Current.Dispatcher.Invoke(() =>
            {
                _Window = (MainWindow)App.GetApp().MainWindow;
                var ServerConfig = new ServerConfig
                {
                    Mode = SocketMode.Tcp,
                    Ip = BindAddress,
                    Port = Port
                };

                // Server Config
                _Server = new WebSocketServer();
                _Server.Setup(new RootConfig(), ServerConfig);
                _Server.NewMessageReceived += _Server_NewMessageReceived;
                _Server.SessionClosed += _Server_SessionClosed;
                _Server.Start();
            });
        }

        protected virtual void Dispose(bool disposing)
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

        #region Private Events
        private void _Server_NewMessageReceived(WebSocketSession Session, string value)
        {
            // Check for Authentication
            if( _Sessions.Contains(Session) ) return;
            try
            {
                var ParsedMessage = JsonConvert.DeserializeObject<SocketChallenge>(value);
                if( _Parent.AuthorizedWebSocketTokens.Contains(ParsedMessage.Token) )
                {
                    // Send success message and initial seed of all messages
                    Session.Send(JsonConvert.SerializeObject(new WebModule.JsonStructure() { error = false }));
                    foreach( var Model in _Window.Model.PingWindows )
                    {
                        Session.Send(JsonConvert.SerializeObject(new WebSockServer.SocketMessage()
                        {
                            IsPaused = (Model.PauseIcon != FontAwesome.WPF.FontAwesomeIcon.Pause),
                            Address = Model.Address.ToString(),
                            Colour = Model.Colour.ToString(),
                            DisplayName = Model.DisplayName,
                            LastContact = Model.LastContact,
                            Alerting = Model.Alerting,
                            Message = "",
                        }));
                    }

                    // Register session to recieve messages
                    _Parent.AuthorizedWebSocketTokens.Remove(ParsedMessage.Token);
                    _Sessions.Add(Session);
                }
            }
            catch( Exception ) {}
            Session.Send(JsonConvert.SerializeObject(new WebModule.JsonStructure() { error = true, result = "Invalid authentication token" }));
        }

        private void _Server_SessionClosed(WebSocketSession Session, CloseReason Value)
        {
            // Close session
            _Sessions.Remove(Session);
        }
        #endregion Private Events

        #region Public Methods
        /// <summary>
        /// Send a message to all connected WebSoscket clients
        /// </summary>
        /// <param name="Message">Message to send</param>
        public void SendSocketMessage(string Message)
        {
            if( _Sessions.Count <= 0 ) return;
            foreach( var Session in _Sessions )
            {
                Task.Run(() => Session.Send(Message));
            }
        }
        #endregion Public Methods
    }

    public class WebCoreBootstrapper : DefaultNancyBootstrapper
    {
        public static string[] StaticDirectories = { "Resources" };
        private byte[] _Favicon;

        #region Favicon
        protected override byte[] FavIcon
        {
            get { return _Favicon ?? ( _Favicon = LoadFavIcon() ); }
        }

        private byte[] LoadFavIcon()
        {
            var ResourcePath = "PingerTool.Resources.favicon.ico";
            using( var Resource = (Assembly.GetExecutingAssembly()).GetManifestResourceStream(ResourcePath) )
            {
                using( var Memory = new MemoryStream() )
                {
                    Resource.CopyTo(Memory);
                    return Memory.GetBuffer();
                }
            }
        }
        #endregion Favicon

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

        /// <summary>
        /// Configure authentication mode
        /// </summary>
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
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

        void OnConfigurationBuilder(NancyInternalConfiguration x)
        {
            #if !DEBUG
            x.ViewLocationProvider = typeof(ResourceViewLocationProvider);
            #endif
        }
        #endregion Override for Embedded Views
    }

    public class WebModule : NancyModule
    {
        public MainWindow _Window;
        private App _AppRef;

        #region JSON Model
        public class JsonStructure
        {
            public bool error { get; set; }
            public dynamic result { get; set; }
        }
        #endregion JSON Model

        #region Custom Module Base
        public WebModule() : base()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _AppRef = App.GetApp();
                _Window = (MainWindow)_AppRef?.MainWindow;
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
        protected bool CheckWhitelisted(string Client)
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
        protected Response ThrowUnauthorized()
        {
            // Return unauthorized response
            return new Response()
            {
                StatusCode = HttpStatusCode.Forbidden,
                Contents = s =>
                {
                    using( var Writer = new StreamWriter(s) )
                    {
                        Writer.WriteLine("HTTP 403 Unauthorized.<br />Your IP is not approved to access this facility");
                    }
                }
            };
        }

        /// <summary>
        /// Return a JSON Response
        /// </summary>
        /// <param name="data">JSON data to send</param>
        /// <param name="Code">HTTP Status code to send</param>
        /// <returns>Response Object</returns>
        protected Response ReturnJson(JsonStructure data, HttpStatusCode Code = HttpStatusCode.OK)
        {
            return new Response
            {
                StatusCode = Code,
                ContentType = "application/json",
                Contents = new Action<Stream>( (Stream) =>
                {
                    try
                    {
                        var JsonData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(data));
                        Stream.Write(JsonData, 0, JsonData.Length);
                    }
                    catch( Exception Ex )
                    {
                        _AppRef.Log.Error(Ex, $"Failed to write response data to a Network Stream {Ex.Message}");
                    }
                })
            };
        }

        /// <summary>
        /// Incoming Function Wrapper
        /// </summary>
        protected dynamic _Wrapper(string Func, dynamic param)
        {
            try
            {
                // Check for Request Body
                if( (param == null || param.Count == 0) && Request.Body.CanRead )
                {
                    using( var SReader = new StreamReader(Request.Body) )
                    {
                        param = JsonConvert.DeserializeObject(SReader.ReadToEnd());
                    }
                }

                // Call Method by Reflection Invoke
                var tFunc = GetType().GetMethod(Func, BindingFlags.NonPublic | BindingFlags.Instance);
                if( tFunc == null ) throw new Exception($"Invalid Method: {Func}");
                return tFunc.Invoke(this, new object[] { param });
            }
            catch( Exception Ex )
            {
                // Exception Handling
                _AppRef.Log.Error(Ex, "Error occurred on API Request: {0}", Ex.Message);
                return ReturnJson(new JsonStructure()
                {
                    result  = ( Ex.InnerException != null ) ? Ex.InnerException.Message : Ex.Message,
                    error   = true
                }, HttpStatusCode.InternalServerError);
            }
        }
        #endregion Custom Module Base
    }
}