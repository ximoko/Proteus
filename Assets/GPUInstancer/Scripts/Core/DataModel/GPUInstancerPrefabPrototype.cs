using System;

namespace GPUInstancer
{
    [Serializable]
    public class GPUInstancerPrefabPrototype : GPUInstancerPrototype
    {
        public bool enableRuntimeModifications;
        public bool startWithRigidBody;
        public bool addRemoveInstancesAtRuntime;
        public int extraBufferSize;
    }
}
