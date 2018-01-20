/*
    PingerTool Web
    Copyright (c) 2018 James Botting - james@bottswanamedia.info
*/

var PingerClass;
var PingerTool = function()
{
    this.AuthenticationToken = "";
    this.PingContainerLines = [];
    this.Authenticated = false;
    this.WebSocket = null;
    self = this;

    // Get Authentication Token
    this.FetchToken = function()
    {
        $.ajax(
        {
            dataType: "json",
            url: "/WebsocketToken",
            success: function(data)
            {
                self.AuthenticationToken = data.result;
                self.InitWebsocket(data.result);
            },
            error: function()
            {
                $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                $(".alert").html("<strong>Authentication Error:</string>Unable to obtain authentication token");                
            }
        });
    };

    // Open Websocket Connection
    this.InitWebsocket = function(Token)
    {
        // Get server location details
        var LocationPort = parseInt(window.location.port)+1;
        var Location = window.location.hostname;

        // Open Websocket
        self.WebSocket = new WebSocket("ws://" + Location + ":" + LocationPort + "/");
        self.WebSocket.onmessage = self.SocketMessage;
        self.WebSocket.onerror = self.SocketError;
        self.WebSocket.onclose = self.SocketClose;
        self.WebSocket.onopen = self.SocketOpen;
    };

    // Send authentication on socket open
    this.SocketOpen = function()
    {
        var Authentication = { "Token" : self.AuthenticationToken };
        self.WebSocket.send(JSON.stringify(Authentication));
    };

    // Handle receipt of messages from server
    this.SocketMessage = function(message)
    {
        // Get message and check for authentication
        var ReceivedMessage = JSON.parse(message.data);
        if( !self.Authenticated )
        {
            if( ReceivedMessage.error )
            {
                $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                $(".alert").html("<strong>Authentication Error:</string>Unable to authenticate socket, " + ReceivedMessage.result);
            }
            else
            {
                $(".alert").css("display", "none");
                self.Authenticated = true;
            }
        }
        else
        {
            // Handle incoming authenticated message
            self.HandleMessage(ReceivedMessage);
        }
    };

    // Handle socket close
    this.SocketClose = function()
    {
        $(".alert").removeClass("alert-info alert-danger").addClass("alert-warning").css("display", "inline-block");
        $(".alert").html("<strong>Disconnected:</strong> The connection to the server has been closed");
    };

    // Socket Error
    this.SocketError = function()
    {
        $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
        $(".alert").html("<strong>Disconnected:</strong> An error occoured with the server connection");
    };

    // Handle successfully received message
    this.HandleMessage = function(message)
    {
        if( message.Message == undefined ) return;
        var TargetContainer = $(".panel[data-address=\"" + message.Address + "\"]");
        if( TargetContainer.length > 0 )
        {
            // Update data in existing element
            TargetContainer.find(".PingData").css("background-color", message.Colour.replace("#FF", "#"));
            TargetContainer.find(".Hostname").html(message.DisplayName + " (" + message.Address + ")");
            TargetContainer.find(".LastContact").html(message.LastContact);
            TargetContainer.attr("data-name", message.DisplayName);
            if( message.IsPaused )
            {
                // Paused containers
                TargetContainer.find(".PingData").html("Ping Control Paused");
                TargetContainer.find(".PauseButton").html("Resume Check");
                self.PingContainerLines[message.Address] = [];
            }
            else if( message.Message.length > 0 )
            {
                if( message.Message == "DELETED" )
                {
                    // Delete container
                    TargetContainer.remove();
                }
                else
                {
                    // Add new entry to array
                    if( self.PingContainerLines[message.Address].length >= 15 ) self.PingContainerLines[message.Address].shift();
                    self.PingContainerLines[message.Address].push(message.Message);

                    // Render array out to display
                    TargetContainer.find(".PingData").html(self.PingContainerLines[message.Address].join("<br />"));
                    TargetContainer.find(".PauseButton").html("Pause Check");
                }
            }
        }
        else
        {
            // Create new element
            var Template = $(".Template .panel").clone();
            Template.attr("data-address", message.Address);
            Template.attr("data-name", message.DisplayName);

            // Place data into the container
            Template.find(".PingData").css("background-color", message.Colour.replace("#FF", "#"));
            Template.find(".Hostname").html(message.DisplayName + " (" + message.Address + ")");
            Template.find(".LastContact").html(message.LastContact);
            Template.attr("data-address", message.Address);

            // Handle paused containers
            self.PingContainerLines[message.Address] = [];
            if( message.IsPaused )
            {
                // Paused containers
                Template.find(".PingData").html("Ping Control Paused");
                Template.find(".PauseButton").html("Resume Check");
            }
            else if( message.Message.length > 0 )
            {
                // Non-paused containers
                Template.find(".PingData").html(message.Message + "<br />");
                self.PingContainerLines[message.Address].push(message.Message);
            }

            // Append to the main container
            $(".Target").append(Template);
        }
    }

    // Add a ping check
    this.AddCheck = function()
    {
        BootstrapDialog.show(
        {
            message: 'Name: &nbsp; <input type="text" style="width:300px;" value="" id="AddBoxName" required/><br /><br />Addr: &nbsp; &nbsp;<input type="text" style="width:300px;" value="" id="AddBoxAddr" required/>',
            title: 'Add Check',
            buttons:
            [
                {
                    label: 'Add',
                    cssClass: 'btn-success',
                    action: function(dialogItself)
                    {
                        var Info = { "addr": $("#AddBoxAddr").val(), "displayname": $("#AddBoxName").val() };
                        $.ajax(
                        {
                            method: "POST",
                            dataType: "json",
                            url: "/AddCheck",
                            data: JSON.stringify(Info),
                            success: function(data)
                            {
                                if( data.error )
                                {
                                    $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                                    $(".alert").html("<strong>Request Failed: </strong> Unable to complete the request: " + data.result);
                                }

                                dialogItself.close();
                            },
                            error: function()
                            {
                                $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                                $(".alert").html("<strong>Request Failed: </strong> Unable to complete the request");
                                dialogItself.close();            
                            }
                        });
                    }
                },
                {
                    label: 'Cancel',
                    action: function(dialogItself)
                    {
                        dialogItself.close();
                    }
                }
            ]
        });   
    }

    // Edit a ping check
    this.EditCheck = function()
    {
        var ThisPanel = $(this).parents(".panel");
        var Address = ThisPanel.attr("data-address");

        if( Address == null || Address.length <= 0 ) return;
        BootstrapDialog.show(
        {
            message: 'Name: &nbsp; <input type="text" style="width:300px;" value="' + ThisPanel.attr("data-name") + '" id="EditBoxName" required/><br /><br />Addr: &nbsp; &nbsp;<input type="text" style="width:300px;" value="' + Address + '" id="EditBoxAddr" required/>',
            title: 'Update Check',
            buttons:
            [
                {
                    label: 'Update',
                    cssClass: 'btn-success',
                    action: function(dialogItself)
                    {
                        var Info = { "oldaddr": Address, "newaddr": $("#EditBoxAddr").val(), "displayname": $("#EditBoxName").val() };
                        $.ajax(
                        {
                            method: "POST",
                            dataType: "json",
                            url: "/EditCheck",
                            data: JSON.stringify(Info),
                            success: function(data)
                            {
                                if( data.error )
                                {
                                    $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                                    $(".alert").html("<strong>Request Failed: </strong> Unable to complete the request: " + data.result);
                                }

                                dialogItself.close();
                            },
                            error: function()
                            {
                                $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                                $(".alert").html("<strong>Request Failed: </strong> Unable to complete the request");
                                dialogItself.close();            
                            }
                        });
                    }
                },
                {
                    label: 'Cancel',
                    action: function(dialogItself)
                    {
                        dialogItself.close();
                    }
                }
            ]
        });   
    }

    // Delete a ping check
    this.DeleteCheck = function()
    {
        var ThisPanel = $(this).parents(".panel");
        var Address = ThisPanel.attr("data-address");

        if( Address == null || Address.length <= 0 ) return;
        BootstrapDialog.show(
        {
            message: 'Are you sure you wish to delete the following check?\n' + ThisPanel.attr("data-name") + ' (' + Address + ')',
            title: 'Confirm Action',
            buttons:
            [
                {
                    label: 'Delete',
                    cssClass: 'btn-success',
                    action: function(dialogItself)
                    {
                        $.ajax(
                        {
                            method: "POST",
                            dataType: "json",
                            url: "/DeleteCheck/" + Address,
                            success: function(data)
                            {
                                if( data.error )
                                {
                                    $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                                    $(".alert").html("<strong>Request Failed: </strong> Unable to complete the request: " + data.result);
                                }

                                dialogItself.close();
                            },
                            error: function()
                            {
                                $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                                $(".alert").html("<strong>Request Failed: </strong> Unable to complete the request");
                                dialogItself.close();            
                            }
                        });
                    }
                },
                {
                    label: 'Cancel',
                    action: function(dialogItself)
                    {
                        dialogItself.close();
                    }
                }
            ]
        });   
    }

    // Toggle the state of a ping check
    this.ToggleCheck = function()
    {
        var ThisPanel = $(this).parents(".panel");
        var Address = ThisPanel.attr("data-address");

        if( Address == null || Address.length <= 0 ) return;
        $.ajax(
        {
            method: "POST",
            dataType: "json",
            url: "/ToggleCheck/" + Address,
            success: function(data)
            {
                if( data.error )
                {
                    $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                    $(".alert").html("<strong>Request Failed: </strong> Unable to complete the request: " + data.result);
                }
            },
            error: function()
            {
                $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                $(".alert").html("<strong>Request Failed: </strong> Unable to complete the request");
                dialogItself.close();            
            }
        }); 
    }

    // Pause All Checks
    this.PauseAllChecks = function()
    {
        BootstrapDialog.show(
        {
            message: 'Are you sure you wish to pause all active ping checks?',
            title: 'Confirm Action',
            buttons:
            [
                {
                    label: 'Continue',
                    cssClass: 'btn-success',
                    action: function(dialogItself)
                    {
                        $.ajax(
                        {
                            method: "POST",
                            dataType: "json",
                            url: "/PauseAllChecks",
                            success: function(data)
                            {
                                if( data.error )
                                {
                                    $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                                    $(".alert").html("<strong>Request Failed: </strong> Unable to complete the request: " + data.result);
                                }

                                dialogItself.close();
                            },
                            error: function()
                            {
                                $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                                $(".alert").html("<strong>Request Failed: </strong> Unable to complete the request");
                                dialogItself.close();            
                            }
                        });
                    }
                },
                {
                    label: 'Cancel',
                    action: function(dialogItself)
                    {
                        dialogItself.close();
                    }
                }
            ]
        });   
    }

    // Resume All Checks
    this.ResumeAllChecks = function()
    {
        BootstrapDialog.show(
        {
            message: 'Are you sure you wish to resume all inactive ping checks?',
            title: 'Confirm Action',
            buttons:
            [
                {
                    label: 'Continue',
                    cssClass: 'btn-success',
                    action: function(dialogItself)
                    {
                        $.ajax(
                        {
                            method: "POST",
                            dataType: "json",
                            url: "/ResumeAllChecks",
                            success: function(data)
                            {
                                if( data.error )
                                {
                                    $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                                    $(".alert").html("<strong>Request Failed: </strong> Unable to complete the request: " + data.result);
                                }

                                dialogItself.close();
                            },
                            error: function()
                            {
                                $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                                $(".alert").html("<strong>Request Failed: </strong> Unable to complete the request");
                                dialogItself.close();            
                            }
                        });
                    }
                },
                {
                    label: 'Cancel',
                    action: function(dialogItself)
                    {
                        dialogItself.close();
                    }
                }
            ]
        });   
    }

    // Save Changes
    this.SaveChanges = function()
    {
        BootstrapDialog.show(
        {
            message: 'Are you sure you wish to save changes to the configuration?\nThe configuration must have been saved once using the local UI first.',
            title: 'Confirm Action',
            buttons:
            [
                {
                    label: 'Continue',
                    cssClass: 'btn-success',
                    action: function(dialogItself)
                    {
                        $.ajax(
                        {
                            method: "POST",
                            dataType: "json",
                            url: "/SaveChanges",
                            success: function(data)
                            {
                                if( data.error )
                                {
                                    $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                                    $(".alert").html("<strong>Request Failed: </strong> Unable to complete the request: " + data.result);
                                }

                                dialogItself.close();
                            },
                            error: function()
                            {
                                $(".alert").removeClass("alert-info alert-warning").addClass("alert-danger").css("display", "inline-block");
                                $(".alert").html("<strong>Request Failed: </strong> Unable to complete the request");
                                dialogItself.close();            
                            }
                        });
                    }
                },
                {
                    label: 'Cancel',
                    action: function(dialogItself)
                    {
                        dialogItself.close();
                    }
                }
            ]
        });
    }

    // Load Token and connect socket
    this.FetchToken();
};


$("body").ready(function()
{
    PingerClass = new PingerTool();
    $(".addCheck").on("click", PingerClass.AddCheck);
    $(".saveChanges").on("click", PingerClass.SaveChanges);
    $(".pauseAllChecks").on("click", PingerClass.PauseAllChecks);
    $(".resumeAllChecks").on("click", PingerClass.ResumeAllChecks);

    $(".Target").on("click", ".editCheck", PingerClass.EditCheck);
    $(".Target").on("click", ".toggleCheck", PingerClass.ToggleCheck);
    $(".Target").on("click", ".deleteCheck", PingerClass.DeleteCheck);
});