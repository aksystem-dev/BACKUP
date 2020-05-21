﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace smart_modul_BACKUP.ServiceInterface {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="ServiceInterface.ISmartModulBackupInterface", CallbackContract=typeof(smart_modul_BACKUP.ServiceInterface.ISmartModulBackupInterfaceCallback), SessionMode=System.ServiceModel.SessionMode.Required)]
    public interface ISmartModulBackupInterface {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/Reload", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/ReloadResponse")]
        void Reload();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/Reload", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/ReloadResponse")]
        System.Threading.Tasks.Task ReloadAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/GetRunningBackups", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/GetRunningBackupsResponse")]
        int[] GetRunningBackups();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/GetRunningBackups", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/GetRunningBackupsResponse")]
        System.Threading.Tasks.Task<int[]> GetRunningBackupsAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/DoSingleBackup", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/DoSingleBackupResponse")]
        SmartModulBackupClasses.BackupInProgress DoSingleBackup(int rule);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/DoSingleBackup", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/DoSingleBackupResponse")]
        System.Threading.Tasks.Task<SmartModulBackupClasses.BackupInProgress> DoSingleBackupAsync(int rule);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/Restore", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/RestoreResponse")]
        SmartModulBackupClasses.RestoreInProgress Restore(SmartModulBackupClasses.Restore restoreInfo);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/Restore", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/RestoreResponse")]
        System.Threading.Tasks.Task<SmartModulBackupClasses.RestoreInProgress> RestoreAsync(SmartModulBackupClasses.Restore restoreInfo);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/GetBackupsInProgress", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/GetBackupsInProgressResponse")]
        SmartModulBackupClasses.BackupInProgress[] GetBackupsInProgress();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/GetBackupsInProgress", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/GetBackupsInProgressResponse")]
        System.Threading.Tasks.Task<SmartModulBackupClasses.BackupInProgress[]> GetBackupsInProgressAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/GetRestoresInProgress", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/GetRestoresInProgressResponse")]
        SmartModulBackupClasses.RestoreInProgress[] GetRestoresInProgress();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/GetRestoresInProgress", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/GetRestoresInProgressResponse")]
        System.Threading.Tasks.Task<SmartModulBackupClasses.RestoreInProgress[]> GetRestoresInProgressAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/Connect", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/ConnectResponse")]
        void Connect();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/Connect", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/ConnectResponse")]
        System.Threading.Tasks.Task ConnectAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/Disconnect", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/DisconnectResponse")]
        void Disconnect();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/Disconnect", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/DisconnectResponse")]
        System.Threading.Tasks.Task DisconnectAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/ImStillHere", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/ImStillHereResponse")]
        void ImStillHere();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/ImStillHere", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/ImStillHereResponse")]
        System.Threading.Tasks.Task ImStillHereAsync();
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ISmartModulBackupInterfaceCallback {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/ShowError", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/ShowErrorResponse")]
        void ShowError(string error);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/TestConnection", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/TestConnectionResponse")]
        void TestConnection();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/ShowMsg", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/ShowMsgResponse")]
        void ShowMsg(string msg);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/Goodbye", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/GoodbyeResponse")]
        void Goodbye();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/StartRestore", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/StartRestoreResponse")]
        void StartRestore(SmartModulBackupClasses.RestoreInProgress progress);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/StartBackup", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/StartBackupResponse")]
        void StartBackup(SmartModulBackupClasses.BackupInProgress progress);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/UpdateRestore", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/UpdateRestoreResponse")]
        void UpdateRestore(SmartModulBackupClasses.RestoreInProgress progress);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/UpdateBackup", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/UpdateBackupResponse")]
        void UpdateBackup(SmartModulBackupClasses.BackupInProgress progress);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/CompleteRestore", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/CompleteRestoreResponse")]
        void CompleteRestore(SmartModulBackupClasses.RestoreInProgress progress, SmartModulBackupClasses.RestoreResponse response);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISmartModulBackupInterface/CompleteBackup", ReplyAction="http://tempuri.org/ISmartModulBackupInterface/CompleteBackupResponse")]
        void CompleteBackup(SmartModulBackupClasses.BackupInProgress progress, int BackupID);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ISmartModulBackupInterfaceChannel : smart_modul_BACKUP.ServiceInterface.ISmartModulBackupInterface, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class SmartModulBackupInterfaceClient : System.ServiceModel.DuplexClientBase<smart_modul_BACKUP.ServiceInterface.ISmartModulBackupInterface>, smart_modul_BACKUP.ServiceInterface.ISmartModulBackupInterface {
        
        public SmartModulBackupInterfaceClient(System.ServiceModel.InstanceContext callbackInstance) : 
                base(callbackInstance) {
        }
        
        public SmartModulBackupInterfaceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName) : 
                base(callbackInstance, endpointConfigurationName) {
        }
        
        public SmartModulBackupInterfaceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress) : 
                base(callbackInstance, endpointConfigurationName, remoteAddress) {
        }
        
        public SmartModulBackupInterfaceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(callbackInstance, endpointConfigurationName, remoteAddress) {
        }
        
        public SmartModulBackupInterfaceClient(System.ServiceModel.InstanceContext callbackInstance, System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(callbackInstance, binding, remoteAddress) {
        }
        
        public void Reload() {
            base.Channel.Reload();
        }
        
        public System.Threading.Tasks.Task ReloadAsync() {
            return base.Channel.ReloadAsync();
        }
        
        public int[] GetRunningBackups() {
            return base.Channel.GetRunningBackups();
        }
        
        public System.Threading.Tasks.Task<int[]> GetRunningBackupsAsync() {
            return base.Channel.GetRunningBackupsAsync();
        }
        
        public SmartModulBackupClasses.BackupInProgress DoSingleBackup(int rule) {
            return base.Channel.DoSingleBackup(rule);
        }
        
        public System.Threading.Tasks.Task<SmartModulBackupClasses.BackupInProgress> DoSingleBackupAsync(int rule) {
            return base.Channel.DoSingleBackupAsync(rule);
        }
        
        public SmartModulBackupClasses.RestoreInProgress Restore(SmartModulBackupClasses.Restore restoreInfo) {
            return base.Channel.Restore(restoreInfo);
        }
        
        public System.Threading.Tasks.Task<SmartModulBackupClasses.RestoreInProgress> RestoreAsync(SmartModulBackupClasses.Restore restoreInfo) {
            return base.Channel.RestoreAsync(restoreInfo);
        }
        
        public SmartModulBackupClasses.BackupInProgress[] GetBackupsInProgress() {
            return base.Channel.GetBackupsInProgress();
        }
        
        public System.Threading.Tasks.Task<SmartModulBackupClasses.BackupInProgress[]> GetBackupsInProgressAsync() {
            return base.Channel.GetBackupsInProgressAsync();
        }
        
        public SmartModulBackupClasses.RestoreInProgress[] GetRestoresInProgress() {
            return base.Channel.GetRestoresInProgress();
        }
        
        public System.Threading.Tasks.Task<SmartModulBackupClasses.RestoreInProgress[]> GetRestoresInProgressAsync() {
            return base.Channel.GetRestoresInProgressAsync();
        }
        
        public void Connect() {
            base.Channel.Connect();
        }
        
        public System.Threading.Tasks.Task ConnectAsync() {
            return base.Channel.ConnectAsync();
        }
        
        public void Disconnect() {
            base.Channel.Disconnect();
        }
        
        public System.Threading.Tasks.Task DisconnectAsync() {
            return base.Channel.DisconnectAsync();
        }
        
        public void ImStillHere() {
            base.Channel.ImStillHere();
        }
        
        public System.Threading.Tasks.Task ImStillHereAsync() {
            return base.Channel.ImStillHereAsync();
        }
    }
}
