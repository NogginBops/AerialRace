using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace AerialRace.Editor
{
    static class Editor
    {
        public static Window Window;

        public static bool InEditorMode;
        public static Camera EditorCamera;
        public static float EditorCameraSpeed = 20;

        public static void InitEditor(Window window)
        {
            Window = window;
            EditorCamera = new Camera(90, window.Width / (float)window.Height, 0.1f, 10000f, Color4.Black);
            EditorCamera.Transform.LocalPosition = new Vector3(0, 5, 5);
        }

        // Called on update while in editor mode
        public static void UpdateEditor(KeyboardState keyboard, MouseState mouse, float deltaTime)
        {


            UpdateEditorCamera(keyboard, mouse, deltaTime);
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
            if (ImGui.Begin("Editor"))
            {


                
            }
            ImGui.End();
        }
    }
}
