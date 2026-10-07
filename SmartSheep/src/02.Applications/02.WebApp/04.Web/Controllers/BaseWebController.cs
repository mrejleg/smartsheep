using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Base.Models.RestApis;
using Project.Core.Extentions;
using Project.Core.Toasts.Abstractions;
using System.Net;
using Web.Controllers.ActionFilters;
using Web.Domain.Models;

namespace Web.Controllers
{
    [Authorize]
    [ActionFilter]
    public class BaseWebController : Controller
    {
        public INotyfService _notyfService;
        public AppSetting _appSetting;

        public BaseWebController(INotyfService notyfService, AppSetting appSetting)
        {
            _notyfService = notyfService;
            _appSetting = appSetting;

        }

        public void AuthorizePage(string accessType)
        {
            HttpContext.Items[ActionFilter.AccessTypeItemKey] = accessType;
        }

        public void AuthorizePage(string controllerName, string accessType)
        {
            HttpContext.Items[ActionFilter.ControllerNameItemKey] = controllerName;
            HttpContext.Items[ActionFilter.AccessTypeItemKey] = accessType;
        }

        [HttpGet]
        public IActionResult DataDxGrid(LoadResult result)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            return Json(result);
        }

        #region WithoutPathView
        public IActionResult Create<TEntity>() where TEntity : new()
        {
            AuthorizePage(AccessTypes.Add.GetEnumDescriptionByValue());

            var model = new RestApiResult<TEntity>();
            return View(model);
        }

        public IActionResult Create<TEntity>(RestApiResult<TEntity> model) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Add.GetEnumDescriptionByValue());

            if (!ModelState.IsValid)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
            }

            if (model == null)
            {
                _notyfService.Error(Messages.RequiredDataMessage);
            }

            return View(model);
        }

        public IActionResult Create<TEntity>(RestApiResult<TEntity> model, RestApiResult<TEntity> result) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Add.GetEnumDescriptionByValue());

            if (ModelState.IsValid)
            {
                if (model != null)
                {
                    if (result.StatusCode == (int)HttpStatusCode.OK)
                    {
                        _notyfService.Success(Messages.SuccessSaveMessage);
                        return RedirectToAction("Index");
                    }
                }
            }

            var modelData = new RestApiResult<TEntity>()
            {
                StatusCode = result.StatusCode,
                Data = model.Data,
            };

            _notyfService.Error(result.Message);
            return View(modelData);
        }

        public IActionResult Edit<TEntity>(object id, RestApiResult<TEntity> result) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Edit.GetEnumDescriptionByValue());

            if (id == null)
            {
                _notyfService.Error(Messages.RequiredDataMessage);
                return RedirectToAction("Index");
            }

            if (result.StatusCode != (int)HttpStatusCode.OK)
            {
                _notyfService.Error(Messages.DataNotFoundMessage);
                return RedirectToAction("Index");
            }

            var model = new RestApiResult<TEntity>()
            {
                StatusCode = result.StatusCode,
                Data = result.Data,
            };

            return View(model);
        }

        public IActionResult Edit<TEntity>(RestApiResult<TEntity> model) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Edit.GetEnumDescriptionByValue());

            if (!ModelState.IsValid)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
            }

            if (model == null)
            {
                _notyfService.Error(Messages.RequiredDataMessage);
            }

            return View(model);
        }

        public IActionResult Edit<TEntity>(RestApiResult<TEntity> model, RestApiResult<TEntity> result) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Edit.GetEnumDescriptionByValue());

            if (ModelState.IsValid)
            {
                if (model != null)
                {
                    if (result.StatusCode == (int)HttpStatusCode.OK)
                    {
                        _notyfService.Success(Messages.SuccessSaveMessage);
                        return RedirectToAction("Index");
                    }
                }
            }

            var modelData = new RestApiResult<TEntity>()
            {
                StatusCode = result.StatusCode,
                Data = model.Data,
            };

            _notyfService.Error(result.Message);
            return View(modelData);
        }

        public IActionResult Details<TEntity>(object id, RestApiResult<TEntity> result) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Detail.GetEnumDescriptionByValue());

            if (id == null)
            {
                _notyfService.Error(Messages.RequiredDataMessage);
                return RedirectToAction("Index");
            }

            if (result.StatusCode != (int)HttpStatusCode.OK)
            {
                _notyfService.Error(Messages.DataNotFoundMessage);
                return RedirectToAction("Index");
            }

            var model = new RestApiResult<TEntity>()
            {
                StatusCode = result.StatusCode,
                Data = result.Data,
            };

            return View(model);
        }

        public IActionResult Delete<TEntity>(RestApiResult<TEntity> result) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Delete.GetEnumDescriptionByValue());

            if (result.StatusCode != (int)HttpStatusCode.OK)
            {
                _notyfService.Error(result.Message);
            }
            else
            {
                _notyfService.Success(Messages.SuccessDeleteMessage);
            }

            return RedirectToAction("Index");
        }
        #endregion

        #region WithPathView
        public IActionResult Create<TEntity>(string pathView) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Add.GetEnumDescriptionByValue());

            var model = new RestApiResult<TEntity>();
            return View(pathView + "Create.cshtml", model);
        }

        public IActionResult CreateValidation<TEntity>(string pathView, RestApiResult<TEntity> model) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Add.GetEnumDescriptionByValue());

            if (!ModelState.IsValid)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
            }

            if (model == null)
            {
                _notyfService.Error(Messages.RequiredDataMessage);
            }

            return View(pathView + "Create.cshtml", model);
        }

        public IActionResult Create<TEntity>(string pathView, RestApiResult<TEntity> model, RestApiResult<TEntity> result) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Add.GetEnumDescriptionByValue());

            if (ModelState.IsValid)
            {
                if (model != null)
                {
                    if (result.StatusCode == (int)HttpStatusCode.OK)
                    {
                        _notyfService.Success(Messages.SuccessSaveMessage);
                        return RedirectToAction("Index");
                    }
                }
            }

            var modelData = new RestApiResult<TEntity>()
            {
                StatusCode = result.StatusCode,
                Data = model.Data,
            };

            _notyfService.Error(result.Message);
            return View(pathView + "Create.cshtml", modelData);
        }

        public IActionResult Edit<TEntity>(string pathView, object id, RestApiResult<TEntity> result) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Edit.GetEnumDescriptionByValue());

            if (id == null)
            {
                _notyfService.Error(Messages.RequiredDataMessage);
                return RedirectToAction("Index");
            }

            if (result.StatusCode != (int)HttpStatusCode.OK)
            {
                _notyfService.Error(Messages.DataNotFoundMessage);
                return RedirectToAction("Index");
            }

            var model = new RestApiResult<TEntity>()
            {
                StatusCode = result.StatusCode,
                Data = result.Data,
            };

            return View(pathView + "Edit.cshtml", model);
        }

        public IActionResult EditValidation<TEntity>(string pathView, RestApiResult<TEntity> model) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Edit.GetEnumDescriptionByValue());

            if (!ModelState.IsValid)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
            }

            if (model == null)
            {
                _notyfService.Error(Messages.RequiredDataMessage);
            }

            return View(pathView + "Edit.cshtml", model);
        }

        public IActionResult Edit<TEntity>(string pathView, RestApiResult<TEntity> model, RestApiResult<TEntity> result) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Edit.GetEnumDescriptionByValue());

            if (ModelState.IsValid)
            {
                if (model != null)
                {
                    if (result.StatusCode == (int)HttpStatusCode.OK)
                    {
                        _notyfService.Success(Messages.SuccessSaveMessage);
                        return RedirectToAction("Index");
                    }
                }
            }

            var modelData = new RestApiResult<TEntity>()
            {
                StatusCode = result.StatusCode,
                Data = model.Data,
            };

            _notyfService.Error(result.Message);
            return View(pathView + "Edit.cshtml", modelData);
        }

        public IActionResult Details<TEntity>(string pathView, object id, RestApiResult<TEntity> result) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Detail.GetEnumDescriptionByValue());

            if (id == null)
            {
                _notyfService.Error(Messages.RequiredDataMessage);
                return RedirectToAction("Index");
            }

            if (result.StatusCode != (int)HttpStatusCode.OK)
            {
                _notyfService.Error(Messages.DataNotFoundMessage);
                return RedirectToAction("Index");
            }

            var model = new RestApiResult<TEntity>()
            {
                StatusCode = result.StatusCode,
                Data = result.Data,
            };

            return View(pathView + "Details.cshtml", model);
        }
        #endregion

        public IActionResult Delete<TEntity>(string pathView, RestApiResult<TEntity> result) where TEntity : new()
        {
            AuthorizePage(AccessTypes.Delete.GetEnumDescriptionByValue());

            if (result.StatusCode != (int)HttpStatusCode.OK)
            {
                _notyfService.Error(result.Message);
            }
            else
            {
                _notyfService.Success(Messages.SuccessDeleteMessage);
            }

            return RedirectToAction("Index");
        }
    }
}
