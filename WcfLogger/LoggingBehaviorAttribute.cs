using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace WcfLogger
{
    public class LoggingBehaviorAttribute : Attribute, IServiceBehavior, IOperationBehavior
    {
        public LoggingBehaviorAttribute()
        {
            LogBeforeCall = true;
            LogAfterCall = true;
            LogErrors = true;
            LogWarnings = true;
            LogInformation = true;
        }

        #region IServiceBehavior Members

        public void AddBindingParameters(ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase,
            Collection<ServiceEndpoint> endpoints,
            BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase)
        {
            LoggingParameterInspector paramInspector = new LoggingParameterInspector
            {
                ServiceType = serviceDescription.ServiceType,
                LoggingStrategy = GetLoggingStrategy(),
                LogAfterCall = LogAfterCall,
                LogBeforeCall = LogBeforeCall,
                LogErrors = LogErrors,
                LogWarnings = LogWarnings,
                LogInformation = LogInformation
            };

            foreach (ChannelDispatcher chDisp in serviceHostBase.ChannelDispatchers)
            {
                foreach (EndpointDispatcher epDisp in chDisp.Endpoints)
                {
                    foreach (DispatchOperation op in epDisp.DispatchRuntime.Operations)
                    {
                        op.ParameterInspectors.Add(paramInspector);
                    }
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase)
        {
        }

        #endregion

        #region IOperationBehavior Members

        public void AddBindingParameters(OperationDescription operationDescription,
            BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(OperationDescription operationDescription,
            ClientOperation clientOperation)
        {
            LoggingParameterInspector paramInspector = new LoggingParameterInspector
            {
                ServiceType = operationDescription.DeclaringContract.ContractType,
                LoggingStrategy = GetLoggingStrategy(),
                LogAfterCall = LogAfterCall,
                LogBeforeCall = LogBeforeCall,
                LogErrors = LogErrors,
                LogWarnings = LogWarnings,
                LogInformation = LogInformation
            };

            clientOperation.ParameterInspectors.Add(paramInspector);
        }

        public void ApplyDispatchBehavior(OperationDescription operationDescription,
            DispatchOperation dispatchOperation)
        {
            LoggingParameterInspector paramInspector =
                dispatchOperation.ParameterInspectors.FirstOrDefault(
                    pi => pi.GetType() == typeof(LoggingParameterInspector)) as LoggingParameterInspector;

            if (paramInspector != null)
            {
                // The logging inspector already exist...

                dispatchOperation.ParameterInspectors.Remove(paramInspector);
            }

            paramInspector = new LoggingParameterInspector
            {
                ServiceType = operationDescription.DeclaringContract.ContractType,
                LoggingStrategy = GetLoggingStrategy(),
                LogAfterCall = LogAfterCall,
                LogBeforeCall = LogBeforeCall,
                LogErrors = LogErrors,
                LogWarnings = LogWarnings,
                LogInformation = LogInformation
            };

            dispatchOperation.ParameterInspectors.Add(paramInspector);
        }

        public void Validate(OperationDescription operationDescription)
        {
        }

        #endregion

        #region Properties

        public bool LogBeforeCall { get; set; }
        public bool LogAfterCall { get; set; }
        public bool LogErrors { get; set; }
        public bool LogWarnings { get; set; }
        public bool LogInformation { get; set; }

        #region LoggingStrategyType
        private Type _loggingStrategyType;
        public Type LoggingStrategyType
        {
            get { return _loggingStrategyType; }
            set
            {
                if (value != null &&
                    !value.GetInterfaces().Contains(typeof(ILoggingStrategy)))
                {
                    throw new ArgumentException("The specified type is not instance of ILoggingStrategy.");
                }

                _loggingStrategyType = value;
            }
        }
        #endregion

        #endregion

        private ILoggingStrategy GetLoggingStrategy()
        {
            return new ConsoleLoggingStrategy();
        }
    }
}
