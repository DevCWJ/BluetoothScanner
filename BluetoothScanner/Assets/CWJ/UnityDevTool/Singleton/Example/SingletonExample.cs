using CWJ;
using CWJ.Singleton;

public class SingletonExample : SingletonBehaviour<SingletonExample>
{
    [InvokeButton]
    private void GetInstance()
    {
        _UpdateInstance();
    }


    protected override void OnInstanceAssigned()
    {
        //Debug.LogWarning($"name : {gameObject.name}\n" + CWJ.ReflectionExtension.GetPrevMethodInfo());
    }

    protected override void _Awake()
    {
        //Debug.LogWarning($"name : {gameObject.name}\n" + CWJ.ReflectionExtension.GetPrevMethodInfo());
    }
}