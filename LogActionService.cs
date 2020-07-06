using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Xml.Linq;
using Microsoft.Practices.ObjectBuilder2;
using Ngn.Extensions;
using Ngn.Helpers;
using Ngn.Infrastructure;
using Ngn.LogAction;
using Ngn.LogAction.DifferenceParsers;
using Ngn.Models;
using Ngn.Repositories.Interfaces;
using Ngn.Services.Interfaces;
using NPOI.SS.Formula.Functions;

namespace Ngn.Services
{
	public class LogActionService : ILogActionService
	{
		#region Constants
		private const string EmailMessageFrom = "admin@admin.ru";

		#endregion


		private const int PageSize = 20;
		private readonly ILogActionRepository _repository;
		private readonly ILogActionSubscriptionService _subscriptionService;
		private EntityDifferenceParser _differenceParser;

		public LogActionService(ILogActionRepository repository, ILogActionSubscriptionService subscriptionService)
		{
			_repository = repository;
			_subscriptionService = subscriptionService;
			_differenceParser = new EntityDifferenceParser();
		}

		public LogActionInfo GetLatestLogAction(int entityId, string entityType, ActionType actionType)
		{
			var result = _repository.GetLogActions(entityId, entityType, actionType).OrderByDescending(s => s.Date).FirstOrDefault();
			FillDiffrences(result);
			return result;
		}

		public PageOfList<LogActionInfo> GetPage(int pageIndex, int? pageSize, SearchCriteria criteria)
		{
			IQueryable<LogActionInfo> query = _repository.GetAll();
			query = ApplyCriteria(query, criteria);
			var result =  query.GetPage(pageIndex, pageSize ?? PageSize);
			result.ForEach(FillDiffrences);
			return result;
		}

		public List<LogActionInfo> GetAllForEntity(int entityId, string entityType)
		{
			var result = _repository.GetAll().Where(s => s.EntityType.ToLower() == entityType.ToLower() && s.EntityId == entityId).OrderByDescending(s => s.Date).ToList();
			result.ForEach(FillDiffrences);
			return result;
		}

		public void SaveLogAction(int entityId, string entityDisplayName, string entityType, ActionType actionType,
								  string comment, string userName, object previousEntity, object currentEntry)
		{
			var currentDate = DateTime.Now;
			_repository.SaveLogAction(entityId, currentDate, entityDisplayName, entityType, actionType, comment, userName, previousEntity,
									  currentEntry);
			NotifySubscribers(currentDate, entityDisplayName, entityType, comment, userName, previousEntity, currentEntry);
		}


		private IQueryable<LogActionInfo> ApplyCriteria(IQueryable<LogActionInfo> query, SearchCriteria criteria)
		{
			if (criteria.Query.IsNotEmpty())
			{
				query =
				  query.Where(
					s => s.EntityDisplayName != null && s.EntityDisplayName.ToLower().Contains(criteria.Query.ToLower()));
			}

			if (criteria.HasCondition("entityType"))
			{
				query =
				  query.Where(
					s => s.EntityType != null && s.EntityType.ToLower() == criteria["entityType"].ToLower());
			}

			if (criteria.HasCondition("username"))
			{
				query =
				  query.Where(
					s => s.UserName != null && s.UserName.ToLower() == criteria["username"].ToLower());
			}

			DateTime? from = criteria.GetNDate("dateFrom");
			if (from.HasValue)
			{
				query = query.Where(s => s.Date >= from.Value);
			}

			DateTime? to = criteria.GetNDate("dateTo");
			if (to.HasValue)
			{
				to = to.Value.AddDays(1).AddMilliseconds(-1);
				query = query.Where(s => s.Date <= to.Value);
			}

			switch (criteria.OrderColumn.ToLowerInvariant())
			{
				case "username":
					query = criteria.Desc
							  ? query.OrderByDescending(s => s.UserName)
							  : query.OrderBy(s => s.UserName);
					break;
				case "entitydisplayname":
					query = criteria.Desc
							  ? query.OrderByDescending(s => s.EntityDisplayName)
							  : query.OrderBy(s => s.EntityDisplayName);
					break;
				case "entitytype":
					query = criteria.Desc
							  ? query.OrderByDescending(s => s.EntityType)
							  : query.OrderBy(s => s.EntityType);
					break;

				case "type":
					query = criteria.Desc
							  ? query.OrderByDescending(s => s.Type)
							  : query.OrderBy(s => s.Type);
					break;
				case "comment":
					query = criteria.Desc
							  ? query.OrderByDescending(s => s.Comment)
							  : query.OrderBy(s => s.Comment);
					break;
				case "date":
					query = criteria.Desc
							  ? query.OrderByDescending(s => s.Date)
							  : query.OrderBy(s => s.Date);
					break;
				default:
					query = query.OrderByDescending(s => s.Date);
					break;
			}
			return query;
		}

		private void FillDiffrences(LogActionInfo actionInfo)
		{
			if (actionInfo == null)
			{
				return;
			}
			actionInfo.Differences = _differenceParser.GetDifferences(actionInfo.CurrentEntity, actionInfo.PreviousEntity);
		}

		private void NotifySubscribers(DateTime date, string entityDisplayName, string entityType, string comment, string userName, object previousEntity, object currentEntry)
		{
			try
			{
				var subscriptions = _subscriptionService.GetAll();

				var subscribers =
					subscriptions.Where(s => s.EntityTypes.Any(ss => ss.Equals(entityType, StringComparison.OrdinalIgnoreCase)))
						.Select(s => s.Subscriber).ToArray();

				if (subscribers.Any())
				{
					MailMessage message = GetMailMessage(date, entityDisplayName, entityType, comment, userName, previousEntity, currentEntry);
					subscribers.ForEach(message.To.Add);
					EmailSender.SendMailInternalAsync(message);
				}
			}
			catch (Exception ex)
			{

			}
			
		}

		private MailMessage GetMailMessage(DateTime date, string entityDisplayName, string entityType, string comment, string userName, object previousEntity, object currentEntry)
		{
			var result = new MailMessage();
			result.From = new MailAddress(EmailMessageFrom, "NotifyFM");
			result.Subject = "Изменения в системе (FinManager)";
            result.Subject += ("Сущность: " + entityType);
            result.Subject += ("Именование: " + entityDisplayName);
            
            //result.Subject = "Извещение";
			var sb = new StringBuilder();
			sb.AppendLine(comment);
			sb.AppendLine("");
			sb.AppendLine(String.Format("Пользователь: {0}", userName));
			sb.AppendLine(String.Format("Дата: {0}", date.ToString("dd.MM.yyyy HH:mm:ss")));
			
			var previousStr = _repository.GetSerializedEntity(previousEntity);
			var currentStr = _repository.GetSerializedEntity(currentEntry);

			var differenses = _differenceParser.GetDifferences(string.IsNullOrEmpty(currentStr) ? null : XElement.Parse(currentStr),
				string.IsNullOrEmpty(previousStr) ? null : XElement.Parse(previousStr)).ToArray();
			if (differenses.Any())
			{
                sb.AppendLine("Сущность: " + entityType);
                sb.AppendLine("Именование: " + entityDisplayName);
                sb.AppendLine("");
				sb.AppendLine("Внесенные изменения:");
				foreach (var differenceInfo in differenses)
				{
					sb.AppendLine(differenceInfo.PropertyName);
					sb.AppendLine(String.Format("Было: {0}", differenceInfo.Previous));
					sb.AppendLine(String.Format("Стало: {0}", differenceInfo.Current));
					sb.AppendLine("");
				}
			}

            result.Body = sb.ToString();
			return result;
		}


	}
}