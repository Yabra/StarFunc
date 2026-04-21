using StarFunc.Data;

namespace StarFunc.Infrastructure
{
    public interface ISaveService
    {
        PlayerSaveData Load();
        void Save(PlayerSaveData data);
        void Delete();
        bool HasSave();
    }
}
