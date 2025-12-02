using System;
using System.Linq;
using VContainer;

namespace Genies.ServiceManagement
{
    public static partial class ServiceManager
    {
        private static void DebugContainerServiceResolving(IContainerBuilder currentBuilder)
        {

            if (currentBuilder == null)
            {
                return;
            }

            //Enable diagnostics for checking if installation is correct in qa and editor
            //It's heavy and increases gc allocations.
#if UNITY_EDITOR || QA_BUILD
            for (int i = 0; i < currentBuilder.Count; i++)
            {
                var registration = currentBuilder[i].Build();

                try
                {
                    LastRootScope.Container.Resolve(registration);
                }
                catch (Exception e)
                {
                    var diagnosticsInfo = currentBuilder.Diagnostics
                                                        .GetDiagnosticsInfos()
                                                        .FirstOrDefault(r => r.RegisterInfo != null && r.RegisterInfo.RegistrationBuilder == currentBuilder[i]);

                    if (diagnosticsInfo != null)
                    {
                        var registerInfo = diagnosticsInfo.RegisterInfo;
                        var message = $"<a href=\"{registerInfo.GetScriptAssetPath()}\" line=\"{registerInfo.GetFileLineNumber()}\">Register at {registerInfo.GetHeadline()}</a>" +
                            Environment.NewLine +
                            Environment.NewLine +
                            registerInfo.StackTrace;

                        throw new ServiceManagerException($"Couldn't Resolve {registration}. Reason: {e.Message}. Registration Trace: {message}");
                    }


                    throw new ServiceManagerException($"Couldn't Resolve {registration}. Reason: {e.Message}.");
                }
            }
#endif
        }
    }
}
