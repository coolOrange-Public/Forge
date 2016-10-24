using System;
using System.Collections.Generic;
using System.Linq;
using Forge.Common;

namespace ForgeConsole
{
	class ActivityDeployment
	{
		private readonly IForgeIoCommunication _communication;
		private readonly List<string> _activities;
		private readonly List<string> _appPackages;

		public ActivityDeployment(IForgeIoCommunication communication)
		{
			_communication = communication;
			_activities = communication.GetActivityDetails().Keys.ToList();
			_appPackages = _communication.GetAppPackageDetails().Keys.ToList();
		}

		public bool Publish(ActivityDefinition activity)
		{
			var activityExists = _activities.Contains(activity.Id);
			if (activityExists)
			{
				Console.WriteLine("Deleting activity: " + activity.Id + " ...");
				_communication.DeleteActivity(activity.Id);
			}

			Console.WriteLine("Creating activity: " + activity.Id + " ...");
			var activityCreated = _communication.CreateActivity(activity);
			Console.WriteLine("Created activity: " + activity.Id + " ("+activityCreated+")");
			if(activityCreated)
				_activities.Add(activity.Id);
			return activityCreated;
		}

		public bool Publish(string appPackageId, string localAppPackage)
		{
			var appPackageExists = _appPackages.Contains(appPackageId);
			if (appPackageExists)
				_communication.DeleteAppPackage(appPackageId);
			var appPackageCreated = _communication.CreateAppPackageFromZip(appPackageId,localAppPackage);
			if (appPackageCreated)
				_appPackages.Add(appPackageId);
			return appPackageCreated;
		}
	}
}