using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Async;

namespace NotFoundMvc
{
    /// <summary>
    /// Wraps another IActionInvoker except it handles the case of an action method
    /// not being found and invokes the NotFoundController instead.
    /// </summary>
    class ActionInvokerWrapper : IAsyncActionInvoker
    {
        readonly IActionInvoker actionInvoker;
        readonly IAsyncActionInvoker asyncInvoker;

        public ActionInvokerWrapper(IActionInvoker actionInvoker)
        {
            this.actionInvoker = actionInvoker;
            asyncInvoker = actionInvoker as IAsyncActionInvoker;
        }

        public bool InvokeAction(ControllerContext controllerContext, string actionName)
        {
            if (InvokeActionWith404Catch(controllerContext, actionName))
                return true;

            // No action method was found, or it was, but threw a 404 HttpException.
            ExecuteNotFoundControllerAction(controllerContext);

            return true;
        }

        static void ExecuteNotFoundControllerAction(ControllerContext controllerContext)
        {
            IController controller;
            if (NotFoundHandler.CreateNotFoundController != null) {
                controller = NotFoundHandler.CreateNotFoundController(controllerContext.RequestContext) ?? new NotFoundController();
            } else {
                controller = new NotFoundController();
            }

            controller.Execute(controllerContext.RequestContext);
        }

        bool InvokeActionWith404Catch(ControllerContext controllerContext, string actionName)
        {
            try {
                return actionInvoker.InvokeAction(controllerContext, actionName);
            } catch (HttpException ex) {
                if (ex.GetHttpCode() == 404) {
                    var skipNotFoundAttributes = controllerContext.Controller.GetType().GetCustomAttributes(typeof(NoMvcNotFoundAttribute), false);
                    return skipNotFoundAttributes.Any();
                }
                throw;
            }
        }

        public IAsyncResult BeginInvokeAction(ControllerContext controllerContext, string actionName, AsyncCallback callback, object state)
        {
            if (asyncInvoker != null)
                return asyncInvoker.BeginInvokeAction(controllerContext, actionName, callback, controllerContext);

            // Just try to invoke the action using the regular action invoker, let the framework throw the right exception for
            // invoking an async function with a non-async invoker.
            InvokeActionWith404Catch(controllerContext, actionName);
            return null;
        }

        public bool EndInvokeAction(IAsyncResult asyncResult)
        {
            if (EndInvokeActionWith404Catch(asyncResult))
                return true;

            ExecuteNotFoundControllerAction(asyncResult.AsyncState as ControllerContext);

            return true;
        }

        bool EndInvokeActionWith404Catch(IAsyncResult asyncResult)
        {
            try {
                return asyncInvoker.EndInvokeAction(asyncResult);
            } catch (HttpException ex) {
                if (ex.GetHttpCode() == 404) {
                    var skipNotFoundAttributes = (asyncResult.AsyncState as ControllerContext).Controller.GetType().GetCustomAttributes(typeof(NoMvcNotFoundAttribute), false);
                    return skipNotFoundAttributes.Any();
                }
                throw;
            }
        }
    }
}
