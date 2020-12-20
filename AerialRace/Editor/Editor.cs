using AerialRace.Debugging;
using AerialRace.Loading;
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
        public static float EditorCameraSpeed = 20;

        public static void InitEditor(Window window)
        {
            Window = window;
            AssetDB = window.AssetDB;
            EditorCamera = new Camera(90, window.Width / (float)window.Height, 0.1f, 10000f, Color4.Black);
            EditorCamera.Transform.LocalPosition = new Vector3(0, 5, 5);

            Gizmos.Init();
        }

        // Called on update while in editor mode
        public static void UpdateEditor(KeyboardState keyboard, MouseState mouse, float deltaTime)
        {
            UpdateEditorCamera(keyboard, mouse, deltaTime);

            Gizmos.UpdateInput(mouse, keyboard, Window.Size, EditorCamera);
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

                EditorCamera.Transform.LocalPosition += direction * EditorCameraSpeed * deltaTime;
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

            if (SelectedTransform != null)
            {
                Gizmos.TransformHandle(SelectedTransform);
            }

            // Gizmos drawlist rendering
            {
                RenderData.RenderDataUtil.UsePipeline(Gizmos.GizmoMaterial.Pipeline);
                EditorCamera.CalcViewProjection(out var vp);
                DrawListSettings settings = new DrawListSettings()
                {
                    DepthTest = false,
                    DepthWrite = false,
                    Vp = vp,
                };
                DrawListRenderer.RenderDrawList(Gizmos.GizmoDrawList, ref settings);
                Gizmos.GizmoDrawList.Clear();
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
                    System.Numerics.Vector3 pos = SelectedTransform.LocalPosition.ToNumerics();
                    if (ImGui.DragFloat3("Position", ref pos, 0.1f))
                        SelectedTransform.LocalPosition = pos.ToOpenTK();

                    // FIXME: Display euler angles
                    //System.Numerics.Vector4 rot = SelectedTransform.LocalRotation.ToNumerics();
                    //if (ImGui.DragFloat4("Rotation", ref rot, 0.1f))
                    //    SelectedTransform.LocalRotation = rot.ToOpenTKQuat();

                    const float MinScale = 0.00000001f;
                    System.Numerics.Vector3 scale = SelectedTransform.LocalScale.ToNumerics();
                    if (ImGui.DragFloat3("Scale", ref scale, 0.1f))
                        SelectedTransform.LocalScale = Vector3.ComponentMax(scale.ToOpenTK(), (MinScale, MinScale, MinScale));

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
    }
}
