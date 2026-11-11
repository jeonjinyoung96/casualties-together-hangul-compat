using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace CasualtiesTogetherHangulCompat
{
    [BepInPlugin(PluginId, "Together Hangul Speech Compatibility", "1.0.0")]
    [BepInProcess("CasualtiesUnknown.exe")]
    [BepInDependency("CasualtiesMP")]
    [BepInDependency("XTMP")]
    public sealed class HangulSpeechCompat : BaseUnityPlugin
    {
        private const string PluginId = "jeonjinyoung96.casualties-together.hangul-speech";
        private const int MaterialBlockLength = 17;

        private void Awake()
        {
            try
            {
                var talker = AccessTools.TypeByName("Talker");
                var renderer = AccessTools.TypeByName("Together.TalkerPixelExempt");
                var target = talker == null || renderer == null
                    ? null
                    : AccessTools.DeclaredMethod(renderer, "SyncOne", new[] { talker });

                if (target == null)
                {
                    Logger.LogWarning("Together's speech renderer was not found. Patch skipped.");
                    return;
                }

                var transpiler = AccessTools.DeclaredMethod(typeof(HangulSpeechCompat), "UseDisplayedFontMaterial");
                new Harmony(PluginId).Patch(target, transpiler: new HarmonyMethod(transpiler));
                Logger.LogInfo("Together overhead speech now uses the displayed font's material.");
            }
            catch (Exception error)
            {
                Logger.LogWarning("Could not patch Together's speech renderer: " + error);
            }
        }

        private static IEnumerable<CodeInstruction> UseDisplayedFontMaterial(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var materialField = AccessTools.Field(typeof(TMP_Asset), "material");
            var getMaterial = AccessTools.PropertyGetter(typeof(TMP_Text), "fontSharedMaterial");
            var setMaterial = AccessTools.PropertySetter(typeof(TMP_Text), "fontSharedMaterial");
            var objectInequality = AccessTools.DeclaredMethod(typeof(UnityEngine.Object), "op_Inequality",
                new[] { typeof(UnityEngine.Object), typeof(UnityEngine.Object) });
            var setFont = AccessTools.PropertySetter(typeof(TMP_Text), "font");

            // The block follows the assignment to the mirror's font and uses the original font's material three times.
            var matches = new List<int>();
            for (int start = 4; start + MaterialBlockLength < code.Count; start++)
            {
                if (!Calls(code[start - 1], setFont) || !Calls(code[start + 16], setMaterial))
                    continue;

                var fontLocal = code[start];
                var mirrorLocal = code[start + 5];
                var uiField = code[start + 6].operand as FieldInfo;
                var afterBlock = code[start + MaterialBlockLength];
                if (LoadsSameLocal(fontLocal, code[start - 2]) &&
                    LoadsSameLocal(fontLocal, code[start + 8]) &&
                    LoadsSameLocal(fontLocal, code[start + 14]) &&
                    LoadsSameLocal(mirrorLocal, code[start - 4]) &&
                    LoadsSameLocal(mirrorLocal, code[start + 12]) &&
                    uiField != null && uiField.FieldType == typeof(TextMeshProUGUI) &&
                    LoadsField(code[start - 3], uiField) &&
                    LoadsField(code[start + 6], uiField) &&
                    LoadsField(code[start + 13], uiField) &&
                    LoadsField(code[start + 1], materialField) &&
                    LoadsField(code[start + 9], materialField) &&
                    LoadsField(code[start + 15], materialField) &&
                    code[start + 2].opcode == OpCodes.Ldnull &&
                    Calls(code[start + 3], objectInequality) &&
                    Calls(code[start + 7], getMaterial) &&
                    Calls(code[start + 10], objectInequality) &&
                    BranchesIfFalseTo(code[start + 4], afterBlock) &&
                    BranchesIfFalseTo(code[start + 11], afterBlock))
                {
                    matches.Add(start);
                }
            }

            if (matches.Count != 1)
                throw new InvalidOperationException("Expected one Together speech material block, found " + matches.Count + ".");

            var blockStart = matches[0];
            var original = code.GetRange(blockStart, MaterialBlockLength);
            if (original.Any(instruction => instruction.blocks.Count != 0) ||
                original.Skip(1).Any(instruction => instruction.labels.Count != 0))
            {
                throw new InvalidOperationException("Together's speech material block has unexpected entry points.");
            }

            var loadMirror = new CodeInstruction(original[5].opcode, original[5].operand);
            loadMirror.labels.AddRange(original[0].labels);
            var loadUi = new CodeInstruction(original[6].opcode, original[6].operand);
            var apply = AccessTools.DeclaredMethod(typeof(HangulSpeechCompat), "ApplyDisplayedFontMaterial");
            code.RemoveRange(blockStart, MaterialBlockLength);
            code.InsertRange(blockStart, new[] { loadMirror, loadUi, new CodeInstruction(OpCodes.Call, apply) });
            return code;
        }

        private static void ApplyDisplayedFontMaterial(TMP_Text ui)
        {
            // XTMP can substitute the font inside LoadFontAsset, which the font setter calls.
            var font = ui.font;
            if (font == null)
                return;

            Material material = font.material;
            if (material != null && ui.fontSharedMaterial != material)
                ui.fontSharedMaterial = material;
        }

        private static bool LoadsSameLocal(CodeInstruction expected, CodeInstruction actual)
        {
            var opcode = expected.opcode;
            return (opcode == OpCodes.Ldloc || opcode == OpCodes.Ldloc_S ||
                    opcode == OpCodes.Ldloc_0 || opcode == OpCodes.Ldloc_1 ||
                    opcode == OpCodes.Ldloc_2 || opcode == OpCodes.Ldloc_3) &&
                actual.opcode == opcode && Equals(expected.operand, actual.operand);
        }

        private static bool LoadsField(CodeInstruction instruction, FieldInfo field)
        {
            return instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, field);
        }

        private static bool Calls(CodeInstruction instruction, MethodInfo method)
        {
            return (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
                Equals(instruction.operand, method);
        }

        private static bool BranchesIfFalseTo(CodeInstruction instruction, CodeInstruction target)
        {
            return (instruction.opcode == OpCodes.Brfalse || instruction.opcode == OpCodes.Brfalse_S) &&
                instruction.operand is Label && target.labels.Contains((Label)instruction.operand);
        }
    }
}
