﻿using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Elements;
using GTA_RP.Vehicles;
using GTA_RP.Jobs;
using GTA_RP.Factions;
using System;
using System.Collections.Generic;
using System.Reflection;
using GTA_RP.Items;

namespace GTA_RP.Events
{
    struct RPEvent
    {
        public RPEvent(object classInstance, string eventName, string method, params Option[] options)
        {
            this.classInstance = classInstance;
            this.method = method;
            this.options = new List<Option>(options);
            this.eventName = eventName;
        }

        public string eventName;
        public object classInstance;
        public string method;
        public List<Option> options;
    }

    enum Option
    {
        OPTION_USES_CHARACTER = 0,
        OPTION_INCLUDE_EVENT = 1
    }

    /// <summary>
    /// A class responsible for handling events that are sent by the client
    /// </summary>
    class EventHandler : Script
    {
        private Dictionary<string, RPEvent> events = new Dictionary<string, RPEvent>();

        public EventHandler()
        {
            API.onClientEventTrigger += HandleEvent;
            InitEvents();
        }

        /// <summary>
        /// Initializes all event handlers
        /// Add all event handlers here
        /// </summary>
        private void InitEvents()
        {
            RegisterEvent("EVENT_REQUEST_ENTER_HOUSE", HouseManager.Instance(), "RequestEnterHouse");
            RegisterEvent("EVENT_REQUEST_EXIT_HOUSE", HouseManager.Instance(), "RequestExitHouse");
            RegisterEvent("EVENT_REQUEST_CREATE_ACCOUNT", PlayerManager.Instance(), "RequestCreateAccount");
            RegisterEvent("EVENT_REQUEST_SELECT_CHARACTER", PlayerManager.Instance(), "RequestSelectCharacter");
            RegisterEvent("EVENT_REQUEST_CREATE_CHARACTER_MENU", PlayerManager.Instance(), "RequestCreateCharacterMenu");
            RegisterEvent("EVENT_REQUEST_CREATE_CHARACTER", PlayerManager.Instance(), "RequestCreateCharacter");
            RegisterEvent("EVENT_REQUEST_OWNED_HOUSES", HouseManager.Instance(), "SendListOfOwnedHousesToClient");
            RegisterEvent("EVENT_SET_PLAYER_USING_PHONE", PlayerManager.Instance(), "SetPlayerUsingPhone");
            RegisterEvent("EVENT_SET_PLAYER_NOT_USING_PHONE", PlayerManager.Instance(), "SetPlayerPhoneOut");
            RegisterEvent("EVENT_SEND_TEXT_MESSAGE", PlayerManager.Instance(), "TrySendTextMessage");
            RegisterEvent("EVENT_ADD_PHONE_CONTACT", PlayerManager.Instance(), "TryAddNewContact");
            RegisterEvent("EVENT_REMOVE_PHONE_CONTACT", PlayerManager.Instance(), "TryDeleteContact");
            RegisterEvent("EVENT_REMOVE_TEXT_MESSAGE", PlayerManager.Instance(), "TryDeleteTextMessage");
            RegisterEvent("EVENT_START_PHONE_CALL", PlayerManager.Instance(), "TryStartPhoneCall");
            RegisterEvent("EVENT_ACCEPT_PHONE_CALL", PlayerManager.Instance(), "TryAcceptPhoneCall");
            RegisterEvent("EVENT_END_PHONE_CALL", PlayerManager.Instance(), "TryHangupPhoneCall");
            RegisterEvent("EVENT_EXIT_VEHICLE_SHOP", VehicleManager.Instance(), "TryExitVehicleShop");
            RegisterEvent("EVENT_BUY_VEHICLE", VehicleManager.Instance(), "TryPurchaseVehicle");
            RegisterEvent("EVENT_TRY_SPAWN_VEHICLE", VehicleManager.Instance(), "SpawnVehicleForCharacter");
            RegisterEvent("EVENT_TRY_PARK_VEHICLE", VehicleManager.Instance(), "ParkVehicle");
            RegisterEvent("EVENT_TRY_LOCK_VEHICLE", VehicleManager.Instance(), "LockVehicleWithId");
            RegisterEvent("EVENT_TRY_BUY_PARKING_SPOT", VehicleManager.Instance(), "TryPurchasePark");
            RegisterEvent("EVENT_TRY_SET_SPAWN_LOCATION", PlayerManager.Instance(), "SetCharacterSpawnHouse");
            RegisterEvent("EVENT_ACCEPT_JOB", JobManager.Instance(), "TakeJobForClient", Option.OPTION_USES_CHARACTER);
            RegisterEvent("EVENT_TRY_BUY_PROPERTY", HouseManager.Instance(), "TryBuyMarketHouseForCharacter", Option.OPTION_USES_CHARACTER);
            RegisterEvent("EVENT_TRY_USE_ITEM", ItemManager.Instance(), "TryUseItemForCharacter", Option.OPTION_USES_CHARACTER);

            /// Faction events
            RegisterEvent("EVENT_ARREST_CHARACTER", FactionManager.Instance().LawEnforcement(), "ArrestCharacter", Option.OPTION_USES_CHARACTER);
        }

        /// <summary>
        /// Registers an event with name
        /// </summary>
        /// <param name="name">Name of the event</param>
        /// <param name="instance">Singleton handler of the event</param>
        /// <param name="methodName">Method that is delegated to handling the event</param>
        /// <param name="needsCharacter">Flag if character is passed to the method instead of the client</param>
        private void RegisterEvent(string name, object instance, string methodName, params Option[] options)
        {
            events.Add(name, new RPEvent(instance, name, methodName, options));
        }

        /// <summary>
        /// Creates the function parameter list for the method that is delegated to some event
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="c">Client</param>
        /// <param name="arguments">Arguments</param>
        /// <returns>Argument list for some event</returns>
        private object[] GetArgumentParametersForEvent(RPEvent e, Client c, params object[] arguments)
        {
            List<object> paramList = new List<object>();

            if (e.options.Contains(Option.OPTION_INCLUDE_EVENT))
                paramList.Add(e.eventName);

            if (e.options.Contains(Option.OPTION_USES_CHARACTER))
            {
                Character character = PlayerManager.Instance().GetActiveCharacterForClient(c);
                paramList.Add(character);
            }
            else
            {
                paramList.Add(c);
            }
            
            for(int i = 0; i < arguments.Length; i++)
            {
                paramList.Add(arguments[i]);
            }


            return paramList.ToArray();
        }

        /// <summary>
        /// Handles a server event
        /// </summary>
        /// <param name="player">Client who sent the event</param>
        /// <param name="eventName">Event name</param>
        /// <param name="arguments">Arguments that client sent</param>
        private void HandleEventWithName(Client player, string eventName, params object[] arguments)
        {
            if (events.ContainsKey(eventName))
            {
                RPEvent eventToInvoke = events[eventName];

                if (eventToInvoke.options.Contains(Option.OPTION_USES_CHARACTER) && !PlayerManager.Instance().IsClientUsingCharacter(player)) return;
                Type type = eventToInvoke.classInstance.GetType();
                MethodInfo method = type.GetMethod(eventToInvoke.method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                object[] prs = GetArgumentParametersForEvent(eventToInvoke, player, arguments);
                method.Invoke(eventToInvoke.classInstance, prs);
            }
        }

        /// <summary>
        /// Main function to handle all kinds of events from the client and forward the message to the relevant classes
        /// </summary>
        /// <param name="player">Sender of the event</param>
        /// <param name="eventName">Name of the event</param>
        /// <param name="arguments">Arguments that are sent with the event</param>
        private void HandleEvent(Client player, string eventName, params object[] arguments)
        {
            HandleEventWithName(player, eventName, arguments);
        }
    }
}
