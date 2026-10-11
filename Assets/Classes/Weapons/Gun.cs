using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    public UnityEvent OnGunShoot;   // Event triggered when the gun is shot
    public float FireCooldown;  // Time in seconds between shots
    public bool isAutomatic;
    private float CurrentCooldown;  // Current cooldown timer

    // Update is called once per frame
    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            bool fireInput = isAutomatic
                ? mouse.leftButton.isPressed
                : mouse.leftButton.wasPressedThisFrame;

            if (fireInput && CurrentCooldown <= 0f)
            {
                OnGunShoot?.Invoke();
                CurrentCooldown = Mathf.Max(0f, FireCooldown);
            }
        }

        CurrentCooldown = Mathf.Max(0f, CurrentCooldown - Time.deltaTime);
    }
}
