using System;
using Nancy;
using System.Net;
using System.Linq;
using System.Windows;
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
            Get["/"] = param => { return _Wrapper("_GetIndexFunction", param); };
            Get["/WebsocketToken"] = param => { return _Wrapper("_GetSocketToken", param); };

            Post["/AddCheck"] = param => { return _Wrapper("_PostAddCheck", param); };
            Post["/EditCheck"] = param => { return _Wrapper("_PostEditCheck", param); };
            Post["/SaveChanges"] = param => { return _Wrapper("_PostSaveChanges", param); };
            Post["/PauseAllChecks"] = param => { return _Wrapper("_PostPauseAllChecks", param); };
            Post["/ResumeAllChecks"] = param => { return _Wrapper("_PostResumeAllChecks", param); };
            Post["/ToggleCheck/{value}"] = param => { return _Wrapper("_PostToggleCheck", param); };
            Post["/DeleteCheck/{value}"] = param => { return _Wrapper("_PostDeleteCheck", param); };
        }
        #endregion Initialisation

        #region GET Webserver Routes
        /// <summary>
        /// Main Webpage
        /// </summary>
        private dynamic _GetIndexFunction(dynamic parameters)
        {
            if( !CheckWhitelisted(Request?.UserHostAddress) ) return ThrowUnauthorized();
            return View["Index.html"];
        }

        /// <summary>
        /// Get token for authenticating with Websocket server
        /// </summary>
        private dynamic _GetSocketToken(dynamic parameters)
        {
            if( !CheckWhitelisted(Request?.UserHostAddress) ) return ThrowUnauthorized();
            var AuthenticationToken = Helpers.SHA256Hash(Guid.NewGuid().ToString());

            _Window.Server.AuthorizedWebSocketTokens.Add(AuthenticationToken);
            return ReturnJson(new JsonStructure()
            {
                result = AuthenticationToken,
                error = false
            });
        }
        #endregion GET Webserver Routes

        #region POST Webserver Routes
        /// <summary>
        /// Pause all active ping checks
        /// </summary>
        private dynamic _PostPauseAllChecks(dynamic parameters)
        {
            if( !CheckWhitelisted(Request?.UserHostAddress) ) return ThrowUnauthorized();
            foreach( var Check in _Window.GetAllElements() )
            {
                if( Check.PauseIcon != FontAwesome.WPF.FontAwesomeIcon.Pause ) continue;
                Application.Current.Dispatcher.Invoke(() => _Window.PausePingElement(Check.Address));
            }

            // Successful operation
            return ReturnJson(new JsonStructure()
            {
                result = null,
                error = false
            });
        }

        /// <summary>
        /// Resume all inactive ping checks
        /// </summary>
        private dynamic _PostResumeAllChecks(dynamic parameters)
        {
            if( !CheckWhitelisted(Request?.UserHostAddress) ) return ThrowUnauthorized();
            foreach( var Check in _Window.GetAllElements() )
            {
                if( Check.PauseIcon == FontAwesome.WPF.FontAwesomeIcon.Pause ) continue;
                Application.Current.Dispatcher.Invoke(() => _Window.ResumePingElement(Check.Address));
            }

            // Successful operation
            return ReturnJson(new JsonStructure()
            {
                result = null,
                error = false
            });
        }

        /// <summary>
        /// Toggle the state of a ping check (Paused/active)
        /// </summary>
        private dynamic _PostToggleCheck(dynamic parameters)
        {
            if( !CheckWhitelisted(Request?.UserHostAddress) ) return ThrowUnauthorized();

            string ThisCheck = parameters["value"] ?? null;
            if( ThisCheck != null && ThisCheck.Length > 0 && IPAddress.TryParse(ThisCheck, out IPAddress Addr) )
            {
                var SpecificCheck = _Window.GetAllElements().Where(q => { return q.Address.Equals(Addr); }).FirstOrDefault();
                if( SpecificCheck != null )
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if( SpecificCheck.PauseIcon != FontAwesome.WPF.FontAwesomeIcon.Pause ) _Window.ResumePingElement(SpecificCheck.Address);
                        else _Window.PausePingElement(SpecificCheck.Address);
                    });

                    // Successful operation
                    return ReturnJson(new JsonStructure()
                    {
                        result = null,
                        error = false
                    });
                }
            }

            // Failed Operation
            return ReturnJson(new JsonStructure()
            {
                result = "Unable to alter the requested ping control",
                error = true
            });
        }

        /// <summary>
        /// Add new Ping Check
        /// </summary>
        private dynamic _PostAddCheck(dynamic parameters)
        {
            if( !CheckWhitelisted(Request?.UserHostAddress) ) return ThrowUnauthorized();
            var DisplayName = (string)parameters["displayname"];

            if( IPAddress.TryParse((string)parameters["addr"], out IPAddress Addr) && DisplayName.Length >= 0 )
            {
                Application.Current.Dispatcher.Invoke(() => _Window.CreatePingElement(DisplayName, Addr));
                return ReturnJson(new JsonStructure()
                {
                    result = null,
                    error = false
                });
            }

            // Failed Operation
            return ReturnJson(new JsonStructure()
            {
                result = "Unable to add the requested ping control",
                error = true
            });
        }

        /// <summary>
        /// Edit an Existing Ping Check
        /// </summary>
        private dynamic _PostEditCheck(dynamic parameters)
        {
            if( !CheckWhitelisted(Request?.UserHostAddress) ) return ThrowUnauthorized();
            var NewDisplayName = (string)parameters["displayname"];

            if( IPAddress.TryParse((string)parameters["oldaddr"], out IPAddress OldAddr) && IPAddress.TryParse((string)parameters["newaddr"], out IPAddress NewAddr) )
            {
                if( NewDisplayName.Length >= 0 )
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if( !OldAddr.Equals(NewAddr) ) _Window.UpdatePingElementAddress(OldAddr, NewAddr);
                        _Window.UpdatePingElementName(OldAddr, NewDisplayName);
                    });

                    // Successful operation
                    return ReturnJson(new JsonStructure()
                    {
                        result = null,
                        error = false
                    });
                }
            }

            // Failed Operation
            return ReturnJson(new JsonStructure()
            {
                result = "Unable to alter the requested ping control",
                error = true
            });
        }

        /// <summary>
        /// Delete an Existing Ping Check
        /// </summary>
        private dynamic _PostDeleteCheck(dynamic parameters)
        {
            if( !CheckWhitelisted(Request?.UserHostAddress) ) return ThrowUnauthorized();

            string ThisCheck = parameters["value"] ?? null;
            if( ThisCheck != null && ThisCheck.Length > 0 && IPAddress.TryParse(ThisCheck, out IPAddress Addr) )
            {
                var SpecificCheck = _Window.GetAllElements().Where(q => { return q.Address.Equals(Addr); }).FirstOrDefault();
                if( SpecificCheck != null )
                {
                    Application.Current.Dispatcher.Invoke(() => _Window.RemovePingElement(SpecificCheck.Address));
                    return ReturnJson(new JsonStructure()
                    {
                        result = null,
                        error = false
                    });
                }
            }

            // Failed Operation
            return ReturnJson(new JsonStructure()
            {
                result = "Unable to delete the requested ping control",
                error = true
            });
        }

        /// <summary>
        /// Save changes to project file
        /// </summary>
        private dynamic _PostSaveChanges(dynamic parameters)
        {
            if( !CheckWhitelisted(Request?.UserHostAddress) ) return ThrowUnauthorized();
            if( _Window.Proj?.sCurrentFile != null && _Window.Proj.sCurrentFile.Length > 0 )
            {
                Application.Current.Dispatcher.Invoke(() => _Window.Proj.SaveProject(_Window.Proj.sCurrentFile));
                return ReturnJson(new JsonStructure()
                {
                    result = null,
                    error = false
                });
            }

            // Failed Operation
            return ReturnJson(new JsonStructure()
            {
                result = "Unable to save changes",
                error = true
            });
        }
        #endregion POST Webserver Routes
    }
}