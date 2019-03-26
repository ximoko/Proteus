using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GPUInstancer
{
    public class GPUInstancerGUIInfo : MonoBehaviour
    {
        public bool showRenderedAmount;

        private void OnGUI()
        {
            if(GPUInstancerManager.activeManagerList != null)
            {
                if(GPUInstancerManager.showRenderedAmount != showRenderedAmount)
                    GPUInstancerManager.showRenderedAmount = showRenderedAmount;

                int startPos = 0;
                int enabledCount = 0;
                string name;

                Color oldColor = GUI.color;
                GUI.color = Color.red;

                foreach (GPUInstancerManager manager in GPUInstancerManager.activeManagerList)
                {
                    enabledCount = 0;
                    name = "detail";
                    if (manager is GPUInstancerPrefabManager)
                    {
                        name = "prefab";
                        enabledCount = ((GPUInstancerPrefabManager)manager).GetEnabledPrefabCount();
                    }
                    DebugOnManagerGUI(name, manager.runtimeDataList, showRenderedAmount, startPos, enabledCount);
                    startPos += GPUInstancerConstants.DEBUG_INFO_SIZE;

                    // Force dispatch compute shader so that rendered amount can be shown correct
                    if (GPUInstancerManager.showRenderedAmount)
                        manager.cameraData.cameraChanged = true;
                }

                GUI.color = oldColor;
            }
        }

        private void OnDisable()
        {
            if(showRenderedAmount)
                GPUInstancerManager.showRenderedAmount = false;
        }

        private static void DebugOnManagerGUI(string name, List<GPUInstancerRuntimeData> runtimeDataList, bool showRenderedAmount, int startPos, int enabledCount)
        {
            if (runtimeDataList == null || runtimeDataList.Count == 0)
            {
                GUI.Label(new Rect(10, Screen.height - startPos - 25, 700, 30),
                    "There are no " + name + " instance prototypes to render!");
                return;
            }

            int totalInstanceCount = 0;
            for (int i = 0; i < runtimeDataList.Count; i++)
            {
                totalInstanceCount += runtimeDataList[i].instanceCount;
            }

            // show instance counts
            GUI.Label(new Rect(10, Screen.height - startPos - 45, 700, 30),
                "Total " + name + " prototype count: " + runtimeDataList.Count);
            GUI.Label(new Rect(10, Screen.height - startPos - 25, 700, 30),
                "Total " + name + " instance count: " + totalInstanceCount);

            if (showRenderedAmount)
            {
                GUI.Label(new Rect(10, Screen.height - startPos - 65, 700, 30),
                    "Rendered " + name + " instance count: " + GetRenderedAmountsGUITextFromArgs(runtimeDataList));
            }

            if (enabledCount > 0)
                GUI.Label(new Rect(10, Screen.height - startPos - 85, 700, 30),
                    "Instancing disabled " + name + " instance count: " + enabledCount);
        }

        private static string GetRenderedAmountsGUITextFromArgs<T>(List<T> runtimeData) where T : GPUInstancerRuntimeData
        {
            int totalRendered = 0;
            int[] lodCounts = new int[5];
            for (int i = 0; i < runtimeData.Count; i++)
            {
                if (runtimeData[i].args != null)
                    for (int lod = 0; lod < runtimeData[i].instanceLODs.Count; lod++)
                        lodCounts[lod] += (int)runtimeData[i].args[runtimeData[i].instanceLODs[lod].argsBufferOffset + 1]
                                              * runtimeData[i].instanceLODs[lod].renderers.Sum(m => m.materials.Count);

            }
            for (int lod = 0; lod < lodCounts.Length; lod++)
                totalRendered += lodCounts[lod];

            return totalRendered + " (LOD0: " + lodCounts[0] + ", LOD1: " + lodCounts[1] +
                                   ", LOD2: " + lodCounts[2] + ", LOD3: " + lodCounts[3] +
                                   ", LOD4: " + lodCounts[4] + ")";
        }
    }
}
