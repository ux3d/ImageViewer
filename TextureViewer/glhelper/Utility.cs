﻿using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using TextureViewer.Models.Shader;

namespace TextureViewer.glhelper
{
    public static class Utility
    {
        public static void GlCheck()
        {
            var glerr = GL.GetError();
            if (glerr != ErrorCode.NoError)
                throw new Exception(glerr.ToString());
        }

        public static void OpenGlDebug(DebugSource source, DebugType type, int id, DebugSeverity severity, int length,
            IntPtr message, IntPtr userParam)
        {


            string str = Marshal.PtrToStringAnsi(message, length);
            //if (str ==
            //    "API_ID_RECOMPILE_FRAGMENT_SHADER performance warning has been generated. Fragment shader recompiled due to state change."
            //)
            //    return;

            if (type != DebugType.DebugTypeOther && // trivial information (buffer is in vram etc.)
                type != DebugType.DebugTypePerformance && // shader recompile
                source != DebugSource.DebugSourceShaderCompiler) // shader compilation error will be handled elsewhere
            {
                App.ShowErrorDialog(null, $"{source}({severity}): {str}");
            }
            // shader compiler will be handled the old way (getProgramInfoLog etc.)
            else if (source != DebugSource.DebugSourceShaderCompiler)
            {
#if DEBUG
                App.ShowInfoDialog(null, $"{source}({severity}): {str}");
#endif
            }
        }

        public static void EnableDebugCallback()
        {
            GL.Enable(EnableCap.DebugOutput);

            GL.Arb.DebugMessageControl(All.DontCare, All.DontCare, All.DontCare, 0, (int[])null, false);
            GL.Arb.DebugMessageControl(All.DontCare, All.DontCare, All.DebugSeverityMedium, 0, (int[])null, true);
            GL.Arb.DebugMessageControl(All.DontCare, All.DontCare, All.DebugSeverityHigh, 0, (int[])null, true);
            GL.Arb.DebugMessageCallback(OpenGlDebug, IntPtr.Zero);
        }
    }
}
