using HarmonyLib;

using BepInEx;
using BepInEx.Harmony;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using AIChara;
using Manager;

using JetBrains.Annotations;

namespace AI_UnlockPlayerHClothes {
    [BepInPlugin(nameof(AI_UnlockPlayerHClothes), nameof(AI_UnlockPlayerHClothes), "1.0.0")]
    public class AI_UnlockPlayerHClothes : BaseUnityPlugin
    {
        private static ChaControl player;
        
        private static readonly List<int> clothesKindList = new List<int>{0, 2, 1, 3, 5, 6};
        
        private void Awake() => HarmonyWrapper.PatchAll(typeof(AI_UnlockPlayerHClothes));

        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")][UsedImplicitly]
        public static void HScene_SetStartVoice_ApplyClothesConfig(HScene __instance)
        {
            var traverse = Traverse.Create(__instance);
            var manager = traverse.Field("hSceneManager").GetValue<HSceneManager>();
            
            if(manager != null && manager.Player != null)
                player = manager.Player.ChaControl;
     
            player.SetClothesState(7, (byte) (!Manager.Config.HData.Shoes ? 2 : 0), true);
            
            foreach (var kind in clothesKindList.Where(kind => player.IsClothesStateKind(kind)))
                player.SetClothesState(kind, (byte)(Manager.Config.HData.Cloth ? 0 : 2), true);
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(HScene), "LateUpdate")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> HScene_LateUpdate_RemoveClothesLock(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            // Force 0 to the if statement, clothes state doesn't get forced anymore //
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "IsClothesStateKind");
            if (index <= 0) return il;

            il[index - 5].opcode = OpCodes.Nop;
            il[index - 4].opcode = OpCodes.Nop;
            il[index - 3].opcode = OpCodes.Nop;
            il[index - 2].opcode = OpCodes.Nop;
            il[index - 1].opcode = OpCodes.Nop;
            il[index].opcode = OpCodes.Ldc_I4_0;

            // Disable forcing shoe state //
            index = il.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "SetAccessoryStateAll");
            if (index <= 0) return il;
            
            for (int i = 1; i < 15; i++)
                il[index + i].opcode = OpCodes.Nop;

            return il;
        }
    }
}
