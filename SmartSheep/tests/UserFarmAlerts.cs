#:project ../src/02.Applications/02.WebApp/04.Web/04.Web.csproj
#:property PublishAot=false
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Base.Models.RestApis;
using Project.Core.Toasts.Abstractions;
using Web.ApplicationCore.Interfaces.Auths;
using Web.ApplicationCore.Interfaces.Configs;
using Web.Controllers.Configs;
using Web.Domain.Models;
using Web.Domain.Models.Configs;

void Check(bool ok,string message) { if(!ok) throw new Exception(message); Console.WriteLine("PASS: "+message); }
foreach(var edit in new[]{false,true}) foreach(var status in new[]{200,409,500}) {
    var alerts=new List<(string Type,string Message)>();
    var model=new RestApiResult<UserFarm> { Data=new UserFarm { FkUserId=Guid.NewGuid(),FkFarmId=Guid.NewGuid() } };
    var response=new RestApiResult<UserFarm> {StatusCode=status,Message=status==409?"Duplicate data":"Service unavailable"};
    var service=Stub.Create<IUserFarmService>((name,args)=>name switch {
        "PostAsync" or "PutAsync" => Task.FromResult(response),
        "GetOptionsAsync" => Task.FromResult(new RestApiResult<UserFarmOptions> {StatusCode=200,Data=new()}),
        _=>throw new Exception("Unexpected service call: "+name)
    });
    var toast=Stub.Create<INotyfService>((name,args)=> { alerts.Add((name,(string)args![0]!));return null; });
    var auth=Stub.Create<IAuthService>((_,_)=>Task.FromResult(true));
    var controller=new UserFarmController(toast,new AppSetting(),service,auth) {ControllerContext=new ControllerContext {HttpContext=new DefaultHttpContext()}};
    var result=edit?await controller.EditAsync(model):await controller.CreateAsync(model);
    if(status==200) {
        Check(result is RedirectToActionResult {ActionName:"Index"},"Successful save redirects to the list");
        Check(alerts.SequenceEqual(new[]{("Success",Messages.SuccessSaveMessage)}),"Create/edit uses the same success alert as App Role");
    } else {
        var view=result as ViewResult;
        Check(view?.ViewName==$"~/Views/Configs/UserFarm/{(edit?"Edit":"Create")}.cshtml" && ((RestApiResult<UserFarm>)view.Model!).Data==model.Data,"API errors preserve the form and selected values");
        Check(alerts.SequenceEqual(new[]{("Error",response.Message)}),"Duplicate/server errors use the same error alert as App Role");
        Check(controller.ModelState.ErrorCount==0,"API errors are not duplicated in an inline validation block");
    }
    alerts.Clear();
    controller.ModelState.AddModelError("Data.FkUserId","Required");
    var invalid=edit?await controller.EditAsync(model):await controller.CreateAsync(model);
    Check(invalid is ViewResult && alerts.SequenceEqual(new[]{("Error",Messages.ModelStateInvalidMessage)}),"Invalid forms use App Role's standard validation alert");
}

public class Stub : DispatchProxy {
    public Func<string,object?[]?,object?> Handler=null!;
    protected override object? Invoke(MethodInfo? method,object?[]? args)=>Handler(method!.Name,args);
    public static T Create<T>(Func<string,object?[]?,object?> handler) where T:class {
        var proxy=Create<T,Stub>(); ((Stub)(object)proxy).Handler=handler;return proxy;
    }
}
