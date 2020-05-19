using SmartModulBackupClasses;

namespace smart_modul_BACKUP
{
    public class SavedSourceSelected
    {
        public SavedSource Value { get; set; }
        public bool Selected { get; set; }
        public string RestorePath { get; set; }
        public bool OverrideSourcePath { get; set; } = false;

        public SavedSourceSelected(SavedSource source)
        {
            Value = source;
            RestorePath = Value.sourcepath;
        }
    }
}
