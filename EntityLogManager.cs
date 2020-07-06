using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ngn.Services.Interfaces;

namespace Ngn.LogAction
{
  public class EntityLogManager : IEntityLogManager
  {
    private string _entityType;
    private ILogActionService _service;
    public EntityLogManager(string entityType, ILogActionService service)
    {
      _entityType = entityType;
      _service = service;
    }




    public string EntityType { get { return _entityType; } }

    public LogActionInfo GetLatestLogAction(int entityId, ActionType actionType)
    {
      return _service.GetLatestLogAction(entityId, _entityType, actionType);
    }

    public List<LogActionInfo> GetLogActions(int entityId)
    {
      return _service.GetAllForEntity(entityId, _entityType);
    }

    public void SaveLogAction(int entityId, string entityDisplayName, ActionType actionType, string comment, string userName, object previousEntity, object currentEntry)
    {
      _service.SaveLogAction(entityId, entityDisplayName, _entityType, actionType, comment, userName, previousEntity, currentEntry);
    }

  }
}
