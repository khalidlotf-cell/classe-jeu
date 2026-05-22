using UnityEngine;
using UnityEngine.InputSystem;

namespace MathsClass
{
    // Contrôleur FPS basique. La sensibilité caméra est lue depuis Settings.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 5.5f;
        public float sprintMultiplier = 1.7f;
        public float baseMouseSensitivity = 0.18f;
        public Transform cam;

        public bool inputEnabled = true;

        CharacterController cc;
        float pitch;
        float yVel;
        float sensitivityMultiplier = 1f;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
        }

        void OnEnable()
        {
            ApplySettings(SaveManager.LoadSettings());
            SaveManager.OnSettingsChanged += ApplySettings;
        }

        void OnDisable()
        {
            SaveManager.OnSettingsChanged -= ApplySettings;
        }

        void ApplySettings(Settings s)
        {
            // 0.5 = neutre. Plage 0..1 → 0.3x..2.5x (0 = très lent, 1 = très rapide)
            sensitivityMultiplier = Mathf.Lerp(0.3f, 2.5f, Mathf.Clamp01(s.cameraSensitivity));
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (!inputEnabled)
            {
                ApplyGravity();
                return;
            }

            Vector2 look = Vector2.zero;
            Vector2 move = Vector2.zero;
            bool sprint = false;

            float sens = baseMouseSensitivity * sensitivityMultiplier;
            if (Mouse.current != null) look = Mouse.current.delta.ReadValue() * sens;
            if (Keyboard.current != null)
            {
                var kb = Keyboard.current;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
                sprint = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            }

            transform.Rotate(0, look.x, 0);
            if (cam)
            {
                pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);
                cam.localEulerAngles = new Vector3(pitch, 0, 0);
            }

            Vector3 dir = transform.right * move.x + transform.forward * move.y;
            if (dir.sqrMagnitude > 1) dir.Normalize();
            float speed = walkSpeed * (sprint ? sprintMultiplier : 1f);

            if (cc.isGrounded && yVel < 0) yVel = -1f;
            yVel += Physics.gravity.y * Time.deltaTime;

            Vector3 motion = dir * speed + Vector3.up * yVel;
            cc.Move(motion * Time.deltaTime);
        }

        void ApplyGravity()
        {
            if (cc.isGrounded && yVel < 0) yVel = -1f;
            yVel += Physics.gravity.y * Time.deltaTime;
            cc.Move(Vector3.up * yVel * Time.deltaTime);
        }

        public void Teleport(Vector3 pos, Quaternion rot)
        {
            cc.enabled = false;
            transform.SetPositionAndRotation(pos, rot);
            pitch = 0;
            if (cam) cam.localEulerAngles = Vector3.zero;
            cc.enabled = true;
        }
    }
}
