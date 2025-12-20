using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Cursor = UnityEngine.Cursor;

public class Player : MonoBehaviour
{

    /*
     * This script is an advanced point & click player controller.
     * When the player !isInMouvement, the camera can rotate around the player (with the mouse), but not moving !
     * When the player clic on something is clickable, the player move toward this object (isInMovement became true) and this is an animation !
     * The position to go to is stocked in every object clicked !
     * To know if an object is usable or a coord to go, it's stocked inside the object too !
     * After that, isInMovement became false again. and that is !
     * 
     * There will be another script we'll check when specific objects are clickable or not. (Like a manager you know) that it i think.
     */

    [Header("Settings")]
    [Tooltip("Reference to the player's camera. (Set up automaticly)")]
    [SerializeField] private Camera _camera;

    [Header("Animation")]
    [Tooltip("Is the player currently in movement?")]
    [SerializeField] private bool isInMovement = false;
    [Tooltip("The speed at which the player moves.")]
    [SerializeField] private float movementSpeed = 5f;

    [Header("Virtual Cursor")]
    [Tooltip("UI cursor (RectTransform) displayed on a Screen Space - Overlay canvas.")]
    [SerializeField] private RectTransform virtualCursor;

    [SerializeField] private float mouseForce = 1f;
    [SerializeField] private float returnForce = 5f;

    [SerializeField] private Vector2 minEllipse = new Vector2(100f, 80f);
    [SerializeField] private Vector2 maxEllipse = new Vector2(400f, 250f);

    // It's for the ramp of the lerp to make enjoyable look
    // TODO: Move that, it's not "cleaan"
    public float temp = 3f;

    [Header("Look")]
    [Tooltip("Mouse sensitivity for camera rotation.")]
    [SerializeField] private float lookSensitivity = 2.5f;


    [Tooltip("Clamp for vertical rotation (pitch).")]
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 80f;

    [Tooltip("DOTween smoothing duration for look rotation.")]
    [SerializeField] private float lookTweenDuration = 0.08f;

    [Header("Cursor")]
    [SerializeField] private Image virtualCursorImage;
    [SerializeField] private Sprite cursorDefault;
    [SerializeField] private Sprite cursorObject;
    [SerializeField] private Sprite cursorMoveTo;

    private PointClickTarget _currentTarget;
    private Vector2 cursorPos;
    private float yaw;
    private float pitch;
    private Tween lookTween;

    void Start()
    {
        _camera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cursorPos = Vector2.zero;

        Vector3 euler = _camera.transform.rotation.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;

    }

    void Update()
    {
        if (!isInMovement) HandleLook();

        // Throw raycast for hover !
        RaycastHover();

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (_currentTarget != null)
                HandleClick(_currentTarget);
        }
    }

    /// <summary>
    /// Process the hover of the cursor.
    /// Enable/Disable the outline, and the cursor sprite
    /// </summary>
    private void RaycastHover()
    {
        Vector2 screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + cursorPos;
        Ray ray = _camera.ScreenPointToRay(screenPoint);

        PointClickTarget newTarget = null;

        if (Physics.Raycast(ray, out RaycastHit hit))
            hit.collider.TryGetComponent(out newTarget);

        // If nothing changed, do nothing (no flicker, no spam)
        if (newTarget == _currentTarget)
            return;

        // Disable previous target visuals
        if (_currentTarget != null)
        {
            _currentTarget.SetOutline(false);
        }

        // Enable new target visuals
        if (newTarget != null)
        {
            newTarget.SetOutline(true);
        }

        // Update cursor sprite (default if null)
        UpdateCursorSprite(newTarget);

        // Cache current
        _currentTarget = newTarget;
    }


    /// <summary>
    /// Manage the cursor sprite, if we hover something and what, and if who hover nothing for reset
    /// </summary>
    /// <param name="target"></param>
    private void UpdateCursorSprite(PointClickTarget target)
    {
        if (virtualCursorImage == null)
            return;

        if (target == null)
        {
            // Reset cursor when hovering nothing
            virtualCursorImage.sprite = cursorDefault;
            return;
        }

        switch (target.InteractionType)
        {
            case InteractionType.Object:
                virtualCursorImage.sprite = cursorObject;
                break;

            case InteractionType.MoveTo:
            default:
                virtualCursorImage.sprite = cursorMoveTo;
                break;
        }
    }


    /// <summary>
    /// Whenused click on an PointClickTarget, we process it
    /// </summary>
    /// <param name="target"></param>
    public void HandleClick(PointClickTarget target)
    {
        Debug.Log($"Player clicked on a PointClickTarget! {target.gameObject.name}");
        //GameObject ob = target.gameObject;

        switch (target.InteractionType)
        {
            case InteractionType.MoveTo:
                // We had clicked on a point to go to ! 


                Move(target);


                return;
            case InteractionType.Object:
                // We had clicked on a object to use

                target.Use();

                return;
            default:
                Debug.Log("I'm gay");
                return;
        }
    }

    public void Move(PointClickTarget target)
    {
        isInMovement = true;

        // Get target transform from the clicked object
        Transform targetTransform = target.GetTransformTarget();
        Transform cam = _camera.transform; // And our cam transform

        // hide cursor
        Color cursorColor = virtualCursor.gameObject.GetComponent<Image>().color;
        Color cursorAlpha = new Color(cursorColor.r, cursorColor.g, cursorColor.b, 0f);
        DOVirtual.Color(cursorColor, cursorAlpha, 0.10f, (value) =>
        {
            virtualCursor.gameObject.GetComponent<Image>().color = value;
        }).OnComplete( () =>
        {
            // Move the cursor at 0 pos (because, we click on position to go to, so the cursor return at the center of the screen !)
            cursorPos = Vector3.zero;
            virtualCursor.anchoredPosition = Vector3.zero;
        });


        //Calc distance from the two transform.
        //The dist make the duration of animations (with an factor of speed)
        float dist = Vector2.Distance(cam.position, targetTransform.position);
        
        cam.DOKill();
        Sequence seq = DOTween.Sequence();
        //TODO: Polish it ! (I'm not sure of the dis/movementSpeed)
        seq.Join(cam.DOMove(targetTransform.position, dist/movementSpeed)); // move
        seq.Join(cam.DORotateQuaternion(targetTransform.rotation, dist/movementSpeed)); // rotate

        seq.OnComplete(() =>
        {

            DOVirtual.Color(cursorAlpha, cursorColor, 0.20f, (value) =>
            {
                virtualCursor.gameObject.GetComponent<Image>().color = value;
            });

            // Set yaw pitch to the right pos
            Vector3 euler = cam.rotation.eulerAngles;
            yaw = euler.y;
            pitch = NormalizeAngle(euler.x);

            // Un hide cursor
            isInMovement = false;
        });
    }

    /// <summary>
    /// Normalize angle (350 -> -10)
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    float NormalizeAngle(float a)
    {
        if (a > 180f) a -= 360f;
        return a;
    }

    public void HandleLook()
    {
        // mouse input
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        // mouse pushes the virtual cursor
        Vector2 mouseDelta = new Vector2(mx, my);
        cursorPos += mouseDelta * mouseForce;

        // compute normalized distances
        float distMin = EllipseDistance(cursorPos, minEllipse);
        float distMax = EllipseDistance(cursorPos, maxEllipse);

        // center attraction force
        if (distMin > 1f)
        {
            float t = Mathf.InverseLerp(1f, temp, distMin);
            Vector2 pullDir = -cursorPos.normalized;
            cursorPos += pullDir * returnForce * t * Time.deltaTime;
        }

        // hard clamp to max ellipse
        if (distMax > 1f)
        {
            cursorPos = ClampToEllipse(cursorPos, maxEllipse);
            distMax = 1f;
        }

        // apply to cursor
        virtualCursor.anchoredPosition = cursorPos;

        // camera follows only outside min ellipse
        if (distMin <= 1f)
            return;

        float speed = Mathf.InverseLerp(1f, temp, distMin);

        yaw += cursorPos.x * lookSensitivity * speed * Time.deltaTime;
        pitch -= cursorPos.y * lookSensitivity * speed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion targetRot = Quaternion.Euler(pitch, yaw, 0f);

        // Animation
        lookTween?.Kill();
        lookTween = _camera.transform
            .DORotateQuaternion(targetRot, lookTweenDuration);
    }

    /// <summary>
    /// Get distance of an position in one ellipse (for cursor and min-max Step)
    /// </summary>
    /// <param name="p"></param>
    /// <param name="ellipse"></param>
    /// <returns></returns>
    float EllipseDistance(Vector2 p, Vector2 ellipse)
    {
        return Mathf.Sqrt(
            (p.x * p.x) / (ellipse.x * ellipse.x) +
            (p.y * p.y) / (ellipse.y * ellipse.y)
        );
    }

    /// <summary>
    /// Clamp the Ellipse distance
    /// </summary>
    /// <param name="p"></param>
    /// <param name="ellipse"></param>
    /// <returns></returns>
    Vector2 ClampToEllipse(Vector2 p, Vector2 ellipse)
    {
        float d = EllipseDistance(p, ellipse);
        if (d <= 1f) return p;
        return p / d;
    }

}
