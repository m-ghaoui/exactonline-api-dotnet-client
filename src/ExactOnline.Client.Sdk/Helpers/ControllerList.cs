using System;
using System.Collections;
using ExactOnline.Client.Sdk.Controllers;
using ExactOnline.Client.Sdk.Interfaces;

namespace ExactOnline.Client.Sdk.Helpers
{
	using System.Collections.Generic;

	using ExactOnline.Client.Models;

	/// <summary>
	/// Manages instances of controllers
	/// </summary>
	public class ControllerList
	{
		private readonly IApiConnector _connector;
		private readonly string _baseUrl;
		private readonly Hashtable _controllers;
		private Dictionary<string, string> _services;

		public ControllerList(IApiConnector connector, string baseUrl)
		{
			_baseUrl = baseUrl;
			_connector = connector;
			_controllers = new Hashtable();
			_services = new Services().Services;
		}

		/// <summary>
		/// Method for getting the correct EntityManager. This method is used as a Delegate
		/// </summary>
		public IEntityManager GetEntityManager(Type type)
		{
			var method = typeof(ControllerList).GetMethod("GetController");
			var genericMethod = method.MakeGenericMethod(new[] { type });
			var controller = (IEntityManager)genericMethod.Invoke(this, null);
			return controller;
		}

		/// <summary>
		/// Returns an instance of Controller with the specified type
		/// </summary>
		public IController<T> GetController<T>() where T : class
		{
			var typename = typeof(T).Name;
			var returncontroller = (Controller<T>)_controllers[typename];

			// If not exists: create
			if (returncontroller == null)
			{
				ApiConnection conn;

				if (typename == "Me")
				{
					conn = new ApiConnection(_connector, _baseUrl + "system/Me");
				}
				else if (_services.ContainsKey(typename))
				{
					conn = new ApiConnection(_connector, _baseUrl + _services[typename]);
				}
				else
				{
					throw new Exception("Specified entity is not known in Exact Online. Please check the reference documentation");
				}

				returncontroller = new Controller<T>(conn)
				{
					GetManagerForEntity = GetEntityManager
				};
				_controllers.Add(typename, returncontroller);
			}

			return returncontroller;
		}

	}
}
