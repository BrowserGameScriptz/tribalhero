﻿#region

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using Game.Data;
using Game.Data.Tribe;
using Game.Map;
using Game.Module;
using Game.Module.Remover;
using Game.Setup;
using Game.Util;
using Game.Util.Locking;
using Game.Util.TwoFactor;
using NDesk.Options;
using Persistance;

#endregion

namespace Game.Comm
{
    class PlayerCommandLineModule : ICommandLineModule
    {
        private readonly Chat chat;

        private readonly ICityRemoverFactory cityRemoverFactory;

        private readonly IDbManager dbManager;

        private readonly ILocker locker;

        private readonly IStructureCsvFactory structureFactory;

        private readonly UnitFactory unitFactory;

        private readonly TechnologyFactory technologyFactory;

        private readonly IPlayersRemoverFactory playerRemoverFactory;

        private readonly IPlayerSelectorFactory playerSelectorFactory;

        private readonly ITribeManager tribeManager;

        private readonly IWorld world;

        public PlayerCommandLineModule(IPlayersRemoverFactory playerRemoverFactory,
                                       IPlayerSelectorFactory playerSelectorFactory,
                                       ICityRemoverFactory cityRemoverFactory,
                                       Chat chat,
                                       IDbManager dbManager,
                                       ITribeManager tribeManager,
                                       IWorld world,
                                       ILocker locker,
                                       IStructureCsvFactory structureFactory,
                                       UnitFactory unitFactory,
                                       TechnologyFactory technologyFactory)
        {
            this.playerRemoverFactory = playerRemoverFactory;
            this.playerSelectorFactory = playerSelectorFactory;
            this.cityRemoverFactory = cityRemoverFactory;
            this.chat = chat;
            this.dbManager = dbManager;
            this.tribeManager = tribeManager;
            this.world = world;
            this.locker = locker;
            this.structureFactory = structureFactory;
            this.unitFactory = unitFactory;
            this.technologyFactory = technologyFactory;
        }

        public void RegisterCommands(CommandLineProcessor processor)
        {
            processor.RegisterCommand("auth", Auth, PlayerRights.Moderator);
            processor.RegisterCommand("resetauthcode", ResetAuthCode, PlayerRights.Bureaucrat);
            processor.RegisterCommand("playerinfo", Info, PlayerRights.Moderator);            
            processor.RegisterCommand("playersearch", Search, PlayerRights.Moderator);
            processor.RegisterCommand("addcoins", AddCoins, PlayerRights.Admin);
            processor.RegisterCommand("ban", BanPlayer, PlayerRights.Moderator);
            processor.RegisterCommand("unban", UnbanPlayer, PlayerRights.Moderator);
            processor.RegisterCommand("deleteplayer", DeletePlayer, PlayerRights.Bureaucrat);
            processor.RegisterCommand("clearplayerdescription", PlayerClearDescription, PlayerRights.Moderator);
            processor.RegisterCommand("deletenewbies", DeleteNewbies, PlayerRights.Bureaucrat);
            processor.RegisterCommand("broadcast", SystemBroadcast, PlayerRights.Bureaucrat);
            processor.RegisterCommand("systemchat", SystemChat, PlayerRights.Bureaucrat);
            processor.RegisterCommand("broadcastmail", SystemBroadcastMail, PlayerRights.Bureaucrat);
            processor.RegisterCommand("setpassword", SetPassword, PlayerRights.Admin);
            processor.RegisterCommand("renameplayer", RenamePlayer, PlayerRights.Admin);
            processor.RegisterCommand("renametribe", RenameTribe, PlayerRights.Admin);
            processor.RegisterCommand("setrights", SetRights, PlayerRights.Bureaucrat);
            processor.RegisterCommand("mute", Mute, PlayerRights.Moderator);
            processor.RegisterCommand("unmute", Unmute, PlayerRights.Moderator);
            processor.RegisterCommand("togglechatmod", ToggleChatMod, PlayerRights.Moderator);
            processor.RegisterCommand("warn", Warn, PlayerRights.Moderator);
            processor.RegisterCommand("setchatlevel", SetChatLevel, PlayerRights.Admin);
            processor.RegisterCommand("giveachievement", GiveAchievement, PlayerRights.Admin);
            processor.RegisterCommand("givesupporterachievement", GiveSupporterAchievement, PlayerRights.Admin);
        }

        public string SetChatLevel(Session session, string[] parms)
        {
            PlayerRights? rights = null;
            var help = false;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},                        
                        {
                                "rights=",
                                v =>
                                rights = (PlayerRights?)Enum.Parse(typeof(PlayerRights), v.TrimMatchingQuotes(), true)
                        }
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || rights == null || !rights.HasValue)
            {
                return String.Format("setchatlevel --rights={0}", String.Join("|", Enum.GetNames(typeof(PlayerRights))));
            }

            Config.chat_min_level = rights.Value;

            return "OK";
        }
        
        private string ToggleChatMod(Session session, string[] parms)
        {
            session.Player.ChatState.Distinguish = !session.Player.ChatState.Distinguish;

            return string.Format("OK, highlighting is now {0}", session.Player.ChatState.Distinguish ? "on" : "off");
        }

        private string SystemChat(Session session, string[] parms)
        {
            bool help = false;
            string message = string.Empty;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"m|message=", v => message = v.TrimMatchingQuotes()},
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(message))
            {
                return "systemchat --message=\"MESSAGE\"";
            }

            chat.SendSystemChat("SYSTEM_CHAT_LITERAL", message);

            return "OK!";
        }

        public string SetRights(Session session, String[] parms)
        {
            bool help = false;
            string playerName = string.Empty;
            PlayerRights? rights = null;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"p=|player=", v => playerName = v.TrimMatchingQuotes()},
                        {
                                "rights=",
                                v =>
                                rights = (PlayerRights?)Enum.Parse(typeof(PlayerRights), v.TrimMatchingQuotes(), true)
                        }
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(playerName) || !rights.HasValue)
            {
                return String.Format("setrights --player=player --rights={0}",
                                     String.Join("|", Enum.GetNames(typeof(PlayerRights))));
            }

            // Kick user out if they are logged in
            uint playerId;
            if (world.FindPlayerId(playerName, out playerId))
            {
                IPlayer player;
                locker.Lock(playerId, out player).Do(() =>
                {
                    if (player != null && player.Session != null)
                    {
                        try
                        {
                            player.Session.CloseSession();
                        }
                        catch
                        {
                        }
                    }
                });
            }

            ApiResponse<dynamic> response = ApiCaller.SetPlayerRights(playerName, rights.GetValueOrDefault());

            return response.Success ? "OK!" : response.AllErrorMessages;
        }

        public string ResetAuthCode(Session session, string[] parms)
        {
            bool help = false;
            string playerName = string.Empty;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"p=|player=", v => playerName = v.TrimMatchingQuotes()}
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(playerName))
            {
                return String.Format("resetauthcode --player=player");
            }

            ApiResponse<dynamic> response = ApiCaller.ResetAuthCode(playerName);

            return response.Success ? "OK!" : response.AllErrorMessages;
        }

        public string Auth(Session session, String[] parms)
        {
            if (session.Player.TwoFactorSecretKey == null)
            {
                return "You must first set up two factor authentication by visiting http://tribalhero.com/mod/players/generate_auth_code . If you've already finished set up, you need to refresh the game once.";
            }

            bool help = false;
            int? code = null;

            try
            {

                var p = new OptionSet
                {
                    {"?|help|h", v => help = true},
                    {"c=|code=", v => code = int.Parse(v.TrimMatchingQuotes())},
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || code == null)
            {
                return String.Format("auth --code=###### (Note: You will be logged out if the code is invalid. If you have trouble authenticating, contact us at giuliano@tribalhero.com)");
            }

            try
            {
                if (!new Totp(session.Player.TwoFactorSecretKey).Verify(code.Value))
                {
                    session.CloseSession();
                    
                    return "Fail";
                }

                session.Player.HasTwoFactorAuthenticated = SystemClock.Now;
                return "Ok. You may use admin commands for one hour or until you log off before you have to re-authenticate.";
            }
            catch(Exception e)
            {
                return string.Format("Fail: {0}", e.Message);
            }
        }

        public string Info(Session session, String[] parms)
        {
            bool help = false;
            string playerName = null;

            try
            {
                
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"p=|player=", v => playerName = v.TrimMatchingQuotes()},
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || (string.IsNullOrEmpty(playerName)))
            {
                return String.Format("playerinfo --player=name|emailaddress");
            }

            ApiResponse<dynamic> response = ApiCaller.PlayerInfo(playerName);

            if (!response.Success)
            {
                return response.AllErrorMessages;
            }

            try
            {
                return
                        String.Format(
                                      "id[{0}] created[{1}] name[{7}] emailAddress[{2}] lastLogin[{3}] ipAddress[{4}] banned[{5}] deleted[{6}]",
                                      response.Data.id,
                                      response.Data.created,
                                      session.Player.Rights > PlayerRights.Moderator ? response.Data.emailAddress : "N/A",
                                      session.Player.Rights > PlayerRights.Moderator ? response.Data.lastLogin : "N/A",
                                      session.Player.Rights > PlayerRights.Moderator ? response.Data.ipAddress : "N/A",
                                      response.Data.banned == "1" ? "YES" : "NO",                                      
                                      response.Data.deleted == "1" ? "YES" : "NO",
                                      response.Data.name);
            }
            catch(Exception e)
            {
                return e.Message;
            }
        }

        public string Search(Session session, String[] parms)
        {
            bool help = false;
            string playerName = null;
            
            try
            {
                
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"o|player=", v => playerName = v.TrimMatchingQuotes()},
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || (string.IsNullOrEmpty(playerName)))
            {
                return String.Format("playersearch --player=name|emailaddress");
            }

            ApiResponse<dynamic> response = ApiCaller.PlayerSearch(playerName);

            if (!response.Success)
            {
                return response.AllErrorMessages;
            }

            try
            {
                return String.Join("\n", response.Data.players);
            }
            catch(Exception e)
            {
                return e.Message;
            }
        }


        public string Mute(Session session, String[] parms)
        {
            bool help = false;
            string playerName = string.Empty;
            int minutes = 10;
            string reason = string.Empty;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"p=|player=", v => playerName = v.TrimMatchingQuotes()},
                        {"m=|minutes=", v => minutes = int.Parse(v.TrimMatchingQuotes()) },
                        {"r=|reason=", v => reason = v.TrimMatchingQuotes() },
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(playerName) || minutes <= 0 || string.IsNullOrEmpty(reason))
            {
                return String.Format("mute --player=player --reason=\"Reason for ban\" --minutes=##");
            }

            uint playerId;
            if (!world.FindPlayerId(playerName, out playerId))
            {
                return "Player not found";
            }

            IPlayer player;
            return locker.Lock(playerId, out player).Do(() =>
            {
                if (player == null)
                {
                    return "Player not found";
                }
                    
                player.Muted = SystemClock.Now.AddMinutes(minutes);
                dbManager.Save(player);

                player.SendSystemMessage(null,
                                         "You have been temporarily muted",
                                         string.Format("You have been temporarily muted for {0} minutes. Reason: {1}\n\n", minutes, reason) +
                                         "Please make sure you are following all of the game rules ( http://tribalhero.com/pages/rules ). " +
                                         "If you have reason to believe this was an unfair judgement, you may contact the game admin directly by email at giuliano@tribalhero.com. Provide as much detail as possible and give us 24 hours to investigate and respond.");

                return string.Format("OK player notified and muted for {0} minutes (until {1})", minutes, player.Muted.ToString("R"));                    
            });
        }

        public string Warn(Session session, String[] parms)
        {
            bool help = false;
            string playerName = string.Empty;
            string reason = string.Empty;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"p=|player=", v => playerName = v.TrimMatchingQuotes()},
                        {"r=|reason=", v => reason = v.TrimMatchingQuotes() },
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(reason))
            {
                return String.Format("warn --player=player --reason=\"Reason for warning\"");
            }

            uint playerId;
            if (!world.FindPlayerId(playerName, out playerId))
            {
                return "Player not found";
            }

            IPlayer player;
            return locker.Lock(playerId, out player).Do(() =>
            {
                if (player == null)
                {
                    return "Player not found";
                }

                var warnMessage = string.Format("You have been warned for misconduct. Reason: {0}\n", reason) +
                                  "Please make sure you are following all of the game rules ( http://tribalhero.com/pages/rules ). If your behavior continues, you may be muted or banned. " +
                                  "If you have reason to believe this was an unfair judgement, you may contact the game admin directly by email at giuliano@tribalhero.com. Provide as much detail as possible and give us 24 hours to investigate and respond.";

                player.SendSystemMessage(null,
                                         "You have been warned for misconduct",
                                         warnMessage);

                if (player.Session != null)
                {
                    chat.SendSystemChat(player.Session, "SYSTEM_CHAT_LITERAL", warnMessage);
                }

                return string.Format("OK player has been warned.");
            });
        }

        public string Unmute(Session session, String[] parms)
        {
            bool help = false;
            string playerName = string.Empty;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"p=|player=", v => playerName = v.TrimMatchingQuotes()},
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(playerName))
            {
                return String.Format("unmute --player=player");
            }

            // Mute player in this world instantly
            uint playerId;
            if (!world.FindPlayerId(playerName, out playerId))
            {
                return "Player not found";
            }

            IPlayer player;
            return locker.Lock(playerId, out player).Do(() =>
            {
                if (player == null)
                {
                    return "Player not found";
                }

                player.Muted = DateTime.MinValue;
                dbManager.Save(player);

                player.SendSystemMessage(null, "You have been unmuted", "You have now been unmuted by a moderator and may talk again in the chat.");

                return "OK!";
            });
        }

        public string RenameTribe(Session session, String[] parms)
        {
            bool help = false;
            string tribeName = string.Empty;
            string newTribeName = string.Empty;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"tribe=", v => tribeName = v.TrimMatchingQuotes()},
                        {"newname=", v => newTribeName = v.TrimMatchingQuotes()}
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(tribeName) || string.IsNullOrEmpty(newTribeName))
            {
                return "renametribe --tribe=name --newname=name";
            }

            uint tribeId;
            if (!tribeManager.FindTribeId(tribeName, out tribeId))
            {
                return "Tribe not found";
            }

            ITribe tribe;
            return locker.Lock(tribeId, out tribe).Do(() =>
            {
                if (tribe == null)
                {
                    return "Tribe not found";
                }

                if (!Tribe.IsNameValid(newTribeName))
                {
                    return "New tribe name is not allowed";
                }

                if (tribeManager.TribeNameTaken(newTribeName))
                {
                    return "New tribe name is already taken";
                }

                tribe.Name = newTribeName;
                dbManager.Save(tribe);

                return "OK!";
            });
        }

        public string SystemBroadcastMail(Session session, String[] parms)
        {
            bool help = false;
            string message = string.Empty;
            string subject = string.Empty;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"s|subject=", v => subject = v.TrimMatchingQuotes()},
                        {"m|message=", v => message = v.TrimMatchingQuotes()},
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(message) || string.IsNullOrEmpty(subject))
            {
                return "broadcastmail --subject=\"SUBJECT\" --message=\"MESSAGE\"";
            }

            using (var reader = dbManager.ReaderQuery(string.Format("SELECT * FROM `{0}`", Player.DB_TABLE),
                                                      new DbColumn[] {}))
            {
                while (reader.Read())
                {
                    IPlayer player;
                    locker.Lock((uint)reader["id"], out player)
                          .Do(() => player.SendSystemMessage(null, subject, message));
                }
            }

            return "OK!";
        }

        public string SystemBroadcast(Session session, String[] parms)
        {
            bool help = false;
            string message = string.Empty;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"m|message=", v => message = v.TrimMatchingQuotes()},
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(message))
            {
                return "broadcast --message=\"MESSAGE\"";
            }

            var packet = new Packet(Command.MessageBox);
            packet.AddString(message);

            Global.Current.Channel.Post("/GLOBAL", packet);
            return "OK!";
        }

        public string PlayerClearDescription(Session session, string[] parms)
        {
            bool help = false;
            string playerName = string.Empty;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"p=|player=", v => playerName = v.TrimMatchingQuotes()}
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(playerName))
            {
                return "playercleardescription --player=player";
            }

            uint playerId;
            if (!world.FindPlayerId(playerName, out playerId))
            {
                return "Player not found";
            }

            IPlayer player;
            return locker.Lock(playerId, out player).Do(() =>
            {
                if (player == null)
                {
                    return "Player not found";
                }

                player.Description = string.Empty;
                player.SendSystemMessage(null,
                                         "Description Clear",
                                         "An administrator has cleared your profile description. If your description was offensive then you may be banned in the future if an innapropriate description is found.");

                return "OK!";
            });
        }

        public string RenamePlayer(Session session, string[] parms)
        {
            bool help = false;
            string playerName = string.Empty;
            string newPlayerName = string.Empty;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"player=", v => playerName = v.TrimMatchingQuotes()},
                        {"newname=", v => newPlayerName = v.TrimMatchingQuotes()}
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(newPlayerName))
            {
                return "renameplayer --player=player --newname=name";
            }

            uint playerId;
            var foundLocally = world.FindPlayerId(playerName, out playerId);

            ApiResponse<dynamic> response = ApiCaller.RenamePlayer(playerName, newPlayerName);

            if (!response.Success)
            {
                return response.AllErrorMessages;
            }

            if (!foundLocally)
            {
                return "Player not found on this server but renamed on main site";
            }

            IPlayer player;
            return locker.Lock(playerId, out player).Do(() =>
            {
                if (player == null)
                {
                    return "Player not found";
                }

                if (player.Session != null)
                {
                    try
                    {
                        player.Session.CloseSession();
                    }
                    catch(Exception)
                    {
                    }
                }

                player.Name = newPlayerName;
                dbManager.Save(player);

                return "OK!";
            });
        }

        public string AddCoins(Session session, string[] parms)
        {
            bool help = false;
            string playerName = string.Empty;
            int coins = 0;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"player=", v => playerName = v.TrimMatchingQuotes()},
                        {"coins=", v => coins = int.Parse(v.TrimMatchingQuotes())}
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(playerName) || coins == 0)
            {
                return "addcoins --player=player --coins=#";
            }

            ApiResponse<dynamic> response = ApiCaller.AddCoins(playerName, coins);

            if (!response.Success)
            {
                return response.AllErrorMessages;
            }

            return "OK!";
        }

        public string SetPassword(Session session, string[] parms)
        {
            bool help = false;
            string playerName = string.Empty;
            string password = string.Empty;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"player=", v => playerName = v.TrimMatchingQuotes()},
                        {"password=", v => password = v.TrimMatchingQuotes()}
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(password))
            {
                return "setpassword --player=player --password=password";
            }

            ApiResponse<dynamic> response = ApiCaller.SetPassword(playerName, password);

            return response.Success ? "OK!" : response.AllErrorMessages;
        }

        public string BanPlayer(Session session, string[] parms)
        {
            bool help = false;
            string playerName = string.Empty;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"p=|player=", v => playerName = v.TrimMatchingQuotes()}
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(playerName))
            {
                return "ban --player=player";
            }

            uint playerId;
            if (!world.FindPlayerId(playerName, out playerId))
            {
                return "Player not found";
            }

            IPlayer player;
            locker.Lock(playerId, out player).Do(() =>
            {
                if (player != null)
                {
                    player.Banned = true;
                    dbManager.Save(player);

                    if (player.Session == null)
                    {
                        return;
                    }

                    try
                    {
                        player.Session.CloseSession();
                    }
                    catch {}
                }
            });

            ApiResponse<dynamic> response = ApiCaller.Ban(playerName);

            return response.Success ? "OK!" : response.AllErrorMessages;
        }

        public string UnbanPlayer(Session session, string[] parms)
        {
            bool help = false;
            string playerName = string.Empty;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"p=|player=", v => playerName = v.TrimMatchingQuotes()}
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(playerName))
            {
                return "unban --player=player";
            }            
            
            uint playerId;
            if (!world.FindPlayerId(playerName, out playerId))
            {
                return "Player not found";
            }

            IPlayer player;
            locker.Lock(playerId, out player).Do(() =>
            {
                if (player != null)
                {
                    player.Banned = false;
                    dbManager.Save(player);
                }
            });

            ApiResponse<dynamic> response = ApiCaller.Unban(playerName);

            return response.Success ? "OK!" : response.AllErrorMessages;
        }

        public string DeletePlayer(Session session, string[] parms)
        {
            bool help = false;
            string playerName = string.Empty;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"p=|player=", v => playerName = v.TrimMatchingQuotes()}
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(playerName))
            {
                return "deleteplayer --player=player";
            }

            uint playerId;
            if (!world.FindPlayerId(playerName, out playerId))
            {
                return "Player not found";
            }

            IPlayer player;
            return locker.Lock(playerId, out player).Do(() =>
            {
                if (player == null)
                {
                    return "Player not found";
                }

                if (player.Session != null)
                {
                    try
                    {
                        player.Session.CloseSession();
                    }
                    catch(Exception)
                    {
                    }
                }

                foreach (ICity city in player.GetCityList())
                {
                    CityRemover cr = cityRemoverFactory.CreateCityRemover(city.Id);
                    cr.Start();
                }

                return "OK!";
            });
        }

        public string DeleteNewbies(Session session, string[] parms)
        {
            bool help = false;

            try
            {
                var p = new OptionSet {{"?|help|h", v => help = true}};

                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help)
            {
                return "deletenewbies";
            }

            PlayersRemover playersRemover =
                    playerRemoverFactory.CreatePlayersRemover(playerSelectorFactory.CreateNewbieIdleSelector());

            return string.Format("OK! Deleting {0} players.", playersRemover.DeletePlayers());
        }

        public string GiveSupporterAchievement(Session session, string[] parms)
        {
            bool help = false;
            string playerName = string.Empty;
            AchievementTier? tier = null;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"p=|player=", v => playerName = v.TrimMatchingQuotes()},
                        {"tier=", v => tier = EnumExtension.Parse<AchievementTier>(v.TrimMatchingQuotes()) },
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || 
                string.IsNullOrEmpty(playerName) || 
                !tier.HasValue)
            {
                return String.Format("givesupporterachievement --player=player --tier={0}",
                                     String.Join("|", Enum.GetNames(typeof(AchievementTier))));
            }

            var type = "SUPPORTER";
            var icon = "coins";
            var title = "Supporter";
            var description = "Helped Improve Tribal Hero";

            ApiResponse<dynamic> response = ApiCaller.GiveAchievement(playerName, tier.Value, type, icon, title, description);

            if (response.Success)
            {
                uint playerId;
                if (world.FindPlayerId(playerName, out playerId))
                {
                    IPlayer player;
                    locker.Lock(playerId, out player).Do(() =>
                    {
                        chat.SendSystemChat("ACHIEVEMENT_NOTIFICATION", playerId.ToString(CultureInfo.InvariantCulture), player.Name, tier.ToString().ToLowerInvariant());

                        player.SendSystemMessage(null, "Achievement", "Hello, I've given you an achievement for supporting us. Your money will help us improve and cover the operating costs of the game. Make sure to refresh the game to see your new achievement. Thanks for your help!");
                    });
                }
            }

            return response.Success ? "OK!" : response.AllErrorMessages;
        }

        public string GiveAchievement(Session session, String[] parms)
        {
            bool help = false;
            string playerName = string.Empty;
            string icon = string.Empty;
            string title = string.Empty;
            string type = string.Empty;
            string description = string.Empty;
            AchievementTier? tier = null;

            try
            {
                var p = new OptionSet
                {
                        {"?|help|h", v => help = true},
                        {"p=|player=", v => playerName = v.TrimMatchingQuotes()},
                        {"title=", v => title = v.TrimMatchingQuotes() },
                        {"description=", v => description = v.TrimMatchingQuotes() },
                        {"icon=", v => icon = v.TrimMatchingQuotes() },
                        {"type=", v => type = v.TrimMatchingQuotes() },
                        {"tier=", v => tier = EnumExtension.Parse<AchievementTier>(v.TrimMatchingQuotes()) },
                };
                p.Parse(parms);
            }
            catch(Exception)
            {
                help = true;
            }

            if (help || 
                string.IsNullOrEmpty(playerName) || 
                string.IsNullOrEmpty(icon) || 
                string.IsNullOrEmpty(title) || 
                string.IsNullOrEmpty(description) || 
                string.IsNullOrEmpty(type) ||
                tier == null ||
                !tier.HasValue)
            {
                return String.Format("giveachievement --player=player --type=type --tier={0} --icon=icon --title=title --description=description",
                                     String.Join("|", Enum.GetNames(typeof(AchievementTier))));
            }

            ApiResponse<dynamic> response = ApiCaller.GiveAchievement(playerName, tier.Value, type, icon, title, description);

            return response.Success ? "OK!" : response.AllErrorMessages;
        }
    }
}