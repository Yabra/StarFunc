namespace StarFunc.Meta
{
    public interface IEconomyService
    {
        int GetFragments();
        void AddFragments(int amount);
        bool SpendFragments(int amount);
        bool CanAfford(int amount);
    }
}
