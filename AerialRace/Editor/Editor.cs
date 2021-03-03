using AerialRace.Debugging;
using AerialRace.Loading;
using AerialRace.RenderData;
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AerialRace.Editor
{
    static class Editor
    {
        public static Window Window;
        public static AssetDB AssetDB;

        public static bool InEditorMode;
        public static Camera EditorCamera;
        public static float EditorCameraSpeed = 100;

        public static void InitEditor(Window window)
        {
            Window = window;
            AssetDB = window.AssetDB;
            EditorCamera = new Camera(/*90*/60, 0.1f, 100000f, Color4.Black);
            EditorCamera.Transform.LocalPosition = new Vector3(0, 5, 5);

            EditorCamera.OrthograpicSize = 200;

            Gizmos.Init();
        }

        // Called on update while in editor mode
        public static void UpdateEditor(KeyboardState keyboard, MouseState mouse, float deltaTime)
        {
            UpdateEditorCamera(keyboard, mouse, deltaTime);
            EditorCamera.Transform.UpdateMatrices();

            EditorCameraSpeed = 100 + EditorCamera.Transform.WorldPosition.Length * 0.5f;

            Gizmos.UpdateInput(mouse, keyboard, Window.Size, EditorCamera);

            bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
            bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
            if (ctrl && keyboard.IsKeyPressed(Keys.Z))
            {
                if (shift)
                     Undo.EditorUndoStack.TryRedo();
                else Undo.EditorUndoStack.TryUndo();
                Console.WriteLine("Undo!!!!");
            }

            if (ctrl && keyboard.IsKeyPressed(Keys.P))
            {
                if (EditorCamera.ProjectionType == ProjectionType.Perspective)
                {
                    EditorCamera.ProjectionType = ProjectionType.Orthographic;
                }
                else
                {
                    EditorCamera.ProjectionType = ProjectionType.Perspective;
                }
            }
        }

        public static float MouseSpeedX = 0.2f;
        public static float MouseSpeedY = 0.2f;
        public static float CameraMinY = -80f;
        public static float CameraMaxY = 80f;
        public static void UpdateEditorCamera(KeyboardState keyboard, MouseState mouse, float deltaTime)
        {
            UpdateCameraMovement(keyboard, deltaTime);
            UpdateCameraDirection(mouse, deltaTime);

            static void UpdateCameraMovement(KeyboardState keyboard, float deltaTime)
            {
                Vector3 direction = Vector3.Zero;

                if (keyboard.IsKeyDown(Keys.W))
                {
                    direction += EditorCamera.Transform.Forward;
                }

                if (keyboard.IsKeyDown(Keys.S))
                {
                    direction += -EditorCamera.Transform.Forward;
                }

                if (keyboard.IsKeyDown(Keys.A))
                {
                    direction += -EditorCamera.Transform.Right;
                }

                if (keyboard.IsKeyDown(Keys.D))
                {
                    direction += EditorCamera.Transform.Right;
                }

                if (keyboard.IsKeyDown(Keys.Space))
                {
                    direction += Vector3.UnitY;
                }

                if (keyboard.IsKeyDown(Keys.LeftShift) |
                    keyboard.IsKeyDown(Keys.RightShift))
                {
                    direction += -Vector3.UnitY;
                }

                float speed = EditorCameraSpeed;
                if (keyboard.IsKeyDown(Keys.LeftControl))
                {
                    speed /= 4f;
                }

                EditorCamera.Transform.LocalPosition += direction * speed * deltaTime;
            }

            static void UpdateCameraDirection(MouseState mouse, float deltaTime)
            {
                if (mouse.IsButtonDown(MouseButton.Right))
                {
                    var delta = mouse.Delta;

                    EditorCamera.YAxisRotation += -delta.X * MouseSpeedX * deltaTime;
                    EditorCamera.XAxisRotation += -delta.Y * MouseSpeedY * deltaTime;
                    EditorCamera.XAxisRotation = MathHelper.Clamp(EditorCamera.XAxisRotation, CameraMinY * Util.D2R, CameraMaxY * Util.D2R);

                    EditorCamera.Transform.LocalRotation = 
                        Quaternion.FromAxisAngle(Vector3.UnitY, EditorCamera.YAxisRotation) *
                        Quaternion.FromAxisAngle(Vector3.UnitX, EditorCamera.XAxisRotation);
                }
            }
        }

        // Called after rendering the scene
        public static void ShowEditor()
        {
            AssetDB.ShowAssetBrowser();

            ShowTransformHierarchy();

            ShowSceneSettings();

            if (SelectedTransform != null)
            {
                Gizmos.TransformHandle(SelectedTransform);
            }

            foreach (var light in Window.Lights.LightsList)
            {
                Gizmos.LightIcon(light);
            }

            Gizmos.CameraGizmo(Window.Player.Camera);

            // Gizmos drawlist rendering
            using (var gizmosOverlayPass = RenderDataUtil.PushGenericPass("Gizmos overlay pass"))
            {
                // We need to enable writing to both depth and color for the clear to work.
                RenderDataUtil.SetDepthWrite(true);
                RenderDataUtil.SetColorWrite(ColorChannels.All);

                // Enable alpha blending
                RenderDataUtil.SetNormalAlphaBlending();

                Screen.ResizeToScreenSizeIfNecessary(Gizmos.GizmosOverlay);

                // FIXME: Make our own enum
                RenderDataUtil.BindDrawFramebufferSetViewportAndClear(
                    Gizmos.GizmosOverlay,
                    default(Color4),
                    OpenTK.Graphics.OpenGL4.ClearBufferMask.ColorBufferBit |
                    OpenTK.Graphics.OpenGL4.ClearBufferMask.DepthBufferBit);

                RenderDataUtil.SetDepthFunc(RenderDataUtil.DepthFunc.PassIfLessOrEqual);

                RenderDataUtil.UsePipeline(Debug.DebugPipeline);
                EditorCamera.CalcViewProjection(out var vp);

                DrawListSettings settings = new DrawListSettings()
                {
                    DepthTest = true,
                    DepthWrite = true,
                    Vp = vp,
                    CullMode = RenderDataUtil.CullMode.Back,
                };
                DrawListRenderer.RenderDrawList(Gizmos.GizmoDrawList, ref settings);
                Gizmos.GizmoDrawList.Clear();

                RenderDataUtil.BindDrawFramebuffer(null);

                // FIXME: Reset viewport

                using (var gizmosOverlayOverlay = RenderDataUtil.PushGenericPass("Scene overlay"))
                {
                    // Here we overlay the gizmo FBO ontop of the default FBO.
                    RenderDataUtil.UsePipeline(Gizmos.GizmoOverlayPipeline);

                    RenderDataUtil.SetNormalAlphaBlending();

                    // FIXME: We might want a better way of binding textures...
                    RenderDataUtil.Uniform1("overlayTex", ShaderStage.Fragment, 0);
                    RenderDataUtil.BindTexture(0, Gizmos.GizmosOverlay.ColorAttachments![0].ColorTexture, (ISampler?)null);

                    RenderDataUtil.DisableAllVertexAttributes();

                    // FIXME: Setup correct blend mode

                    OpenTK.Graphics.OpenGL4.GL.DrawArrays(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, 0, 3);

                    // Important to unbind this texture so that we can draw to it later.
                    RenderDataUtil.BindTexture(0, null);

                    // Just in case we disable blending.
                    RenderDataUtil.DisableBlending();
                }
            }
        }

        public static Transform? SelectedTransform;
        public static void ShowTransformHierarchy()
        {
            if (ImGui.Begin("Hierarchy", ImGuiWindowFlags.NoFocusOnAppearing))
            {
                ImGui.Columns(2);

                // FIXME!!!!! Keep a list of roots...
                foreach (var t in Transform.Transforms.Where(t => t.Parent == null))
                {
                    ShowTransform(t);
                }

                ImGui.NextColumn();

                if (SelectedTransform != null)
                {
                    var pos = SelectedTransform.LocalPosition;
                    if (ImGui.DragFloat3("Position", ref SelectedTransform.LocalPosition.AsNumerics(), 0.1f))
                    {
                        Undo.EditorUndoStack.PushAlreadyDone(new Translate()
                        {
                            Transform = SelectedTransform,
                            StartPosition = pos,
                            EndPosition = SelectedTransform.LocalPosition
                        });
                    }

                    // FIXME: Display euler angles
                    //System.Numerics.Vector4 rot = SelectedTransform.LocalRotation.ToNumerics();
                    //if (ImGui.DragFloat4("Rotation", ref rot, 0.1f))
                    //    SelectedTransform.LocalRotation = rot.ToOpenTKQuat();

                    const float MinScale = 0.00000001f;
                    var scale = SelectedTransform.LocalScale;
                    if (ImGui.DragFloat3("Scale", ref SelectedTransform.LocalScale.AsNumerics(), 0.1f, MinScale))
                    {
                        Undo.EditorUndoStack.PushAlreadyDone(new Scale()
                        {
                            Transform = SelectedTransform,
                            StartScale = scale,
                            EndScale = SelectedTransform.LocalScale
                        });
                    }

                    Light? light = Window.Lights.LightsList.Find(l => l.Transform == SelectedTransform);
                    if (light != null)
                    {
                        ImGui.DragFloat("Radius", ref light.Radius, 1, 0.01f, 10000, null, ImGuiSliderFlags.ClampOnInput);

                        ImGui.ColorEdit3("Intensity", ref light.Intensity.AsNumerics(), ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.Float);

                        ImGui.DragFloat("Intensity (candela)", ref light.Candela, 1, 0, 100000);

                        float error = 1 - Light.CalculatePointLightPercentageAccuracyFromRadius(light.Radius);
                        ImGui.Text($"Error: {error * 100:0.00000}%% ({light.Candela * error:0.00000} units)");
                    }
                }
                else
                {
                    ImGui.Text("No transform selected");
                }
            }
            ImGui.End();

            static void ShowTransform(Transform transform)
            {
                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (SelectedTransform == transform)
                {
                    flags |= ImGuiTreeNodeFlags.Selected;
                }

                if (transform.Children?.Count > 0)
                {
                    bool open = ImGui.TreeNodeEx(transform.Name, flags);

                    if (ImGui.IsItemClicked())
                    {
                        SelectedTransform = transform;
                    }

                    if (open)
                    {
                        for (int i = 0; i < transform.Children.Count; i++)
                        {
                            ShowTransform(transform.Children[i]);
                        }

                        ImGui.TreePop();
                    }
                }
                else
                {
                    flags |= ImGuiTreeNodeFlags.Leaf;
                    ImGui.TreeNodeEx(transform.Name, flags);

                    if (ImGui.IsItemClicked())
                    {
                        SelectedTransform = transform;
                    }

                    ImGui.TreePop();
                }
            }
        }

        public static void ShowSceneSettings()
        {
            if (ImGui.Begin("Scene settings"))
            {
                EnumCombo("Tonemap", ref Window.CurrentTonemap);
                ImGui.DragFloat("Light cutoff", ref Window.LightCutout, 0.001f, 0, 0.5f);

                var flags = ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.Float;
                ImGui.ColorEdit3("Sun color", ref Window.Sky.SunColor.AsNumerics3(), flags);
                ImGui.ColorEdit3("Sky color", ref Window.Sky.SkyColor.AsNumerics3(), flags);
                ImGui.ColorEdit3("Ground color", ref Window.Sky.GroundColor.AsNumerics3(), flags);
            }
            ImGui.End();

            static void EnumCombo<T>(string label, ref T currValue) where T : struct, Enum
            {
                string[] names = Enum.GetNames<T>();
                int selectedIndex = Unsafe.As<T, int>(ref currValue);
                if (ImGui.BeginCombo(label, names[selectedIndex]))
                {
                    for (int i = 0; i < names.Length; i++)
                    {
                        bool selected = i == selectedIndex;
                        if (ImGui.Selectable(names[i], selected))
                        {
                            currValue = Enum.GetValues<T>()[i];
                        }

                        if (selected) ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
                
            }
        }
    }
}
