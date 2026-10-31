namespace Module;

// 全部模板的配置(每张地图重新注册)。模板按实际注册数量存储,不预留固定上限。
public sealed class WeaponConfig
{
    // 未注册序号的读取统一落到这个空模板,避免为越界访问分配对象
    private static readonly WeaponData Empty = new();

    private readonly List<WeaponData> _templates = new() { new WeaponData() }; // 0 号占位,模板序号从 1 开始
    private readonly Dictionary<int, int>[] _serialNumbers = { new(), new(), new() }; // [slot] INI 序号 -> 模板序号

    public int WeaponAmount => _templates.Count - 1;

    public bool CanRegister => WeaponAmount < WpnConst.MaxTemplateCount;

    public bool IsTemplate(int index) => index > 0 && index < _templates.Count;

    public WeaponData this[int index] => IsTemplate(index) ? _templates[index] : Empty;

    public bool TryCreate(out int index)
    {
        index = 0;
        if (!CanRegister) return false;
        index = _templates.Count;
        _templates.Add(new WeaponData());
        return true;
    }

    public bool HasSerial(int slot, int serial) => _serialNumbers[slot].ContainsKey(serial);

    public void SetSerial(int slot, int serial, int index) => _serialNumbers[slot][serial] = index;

    public int GetTemplateBySerial(int slot, int serial) =>
        _serialNumbers[slot].TryGetValue(serial, out int index) ? index : 0;

    public void Reset()
    {
        _templates.Clear();
        _templates.Add(new WeaponData());
        foreach (var serialNumbers in _serialNumbers) serialNumbers.Clear();
    }
}
