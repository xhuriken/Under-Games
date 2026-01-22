using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Cursor = UnityEngine.Cursor;

public class PCController : MonoBehaviour
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

    public static PCController Instance;

    [Header("Settings")]
    [Tooltip("Reference to the player's camera. (Set up automaticly)")]
    [SerializeField] private Camera _camera;

    [Header("Animation")]
    [Tooltip("Is the player currently in movement?")]
    [SerializeField] private bool isInMovement = false;
    [Tooltip("The speed at which the player moves.")]
    [SerializeField] private float moveSpeed = 5f;   
    [Tooltip("The speed at which the player rotate (Juste for the move()).")]
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private Transform cameraRig;

    [Header("Virtual Cursor")]
    [Tooltip("UI cursor (RectTransform) displayed on a Screen Space - Overlay canvas.")]
    [SerializeField] private RectTransform virtualCursor;

    [SerializeField] private float mouseForce = 1f;
    [SerializeField] private float returnForce = 5f;

    [SerializeField] private Vector2 minEllipse = new Vector2(100f, 80f);
    [SerializeField] private Vector2 maxEllipse = new Vector2(400f, 250f);

    // It's for the ramp of the lerp to make enjoyable look
    [SerializeField] private float rampFactor = 3f;

    [Header("Look")]
    [Tooltip("Mouse sensitivity for camera rotation.")]
    [SerializeField] private float lookSensitivity = 2.5f;

    [Tooltip("Clamp for vertical rotation (pitch).")]
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 80f;

    [Tooltip("DOTween smoothing duration for look rotation.")]
    [SerializeField] private float lookTweenDuration = 0.08f;

    [Tooltip("If true, the player's look is locked and cannot rotate.")]
    [SerializeField] public bool isLookLocked = false;
    
    private PointClickTarget _currentHoveringTarget;
    private PointClickTarget currentAnchor;
    [SerializeField]private PointClickTarget usedObject;
    public PointClickTarget CurrentAnchor => currentAnchor;

    [Header("State")]
    private Vector2 cursorPos;
    private float yaw;
    private float pitch;
    private float roll;
    private Tween lookTween;

    private void Awake()
    {
        if(Instance == null) Instance = this;
    }

    void Start()
    {
        cursorPos = Vector2.zero;

        Vector3 euler = cameraRig.eulerAngles;
        yaw = euler.y;
        pitch = NormalizeAngle(euler.y);
        roll = NormalizeAngle(euler.z);
        //Subscribe to beat
        MusicManager.beatUpdated += Shake;
    }

    void Update()
    {
        if (!isInMovement && !isLookLocked) HandleLook();

        // Throw raycast for hover !
        RaycastHover();

        // Key DOWN
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (_currentHoveringTarget != null && CanInteract(_currentHoveringTarget) && !isInMovement)
                HandleClick(_currentHoveringTarget);

            if(_currentHoveringTarget == null) usedObject = null;
        }

        // Key UP
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            if (usedObject != null && /*CanInteract(_currentTarget) &&*/ !isInMovement)
                HandleClick(usedObject, true);
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

        if (newTarget != null && !CanInteract(newTarget))
            newTarget = null;

        if (usedObject != null || isInMovement)
        {
            _currentHoveringTarget?.SetOutline(false);
            newTarget?.SetOutline(false);
            return;
        }

        // If nothing changed, do nothing (no flicker, no spam)
        // if the new target is the same as the current one, and if we are not in movement we stop here
        //if (newTarget == _currentHoveringTarget || isInMovement)
        //    return;

        // If we're here, the target changed ! (or we're in movement)
        // the latest = _currentHoveringTarget
        // the new one = newTarget

        // Disable previous target visuals
        // if current target is not null (so the last technicly, because the real target is "newTarget")
        if (_currentHoveringTarget != null)
        {
            _currentHoveringTarget.SetOutline(false);
        }

        // Enable new target visuals
        // if target is not nul, if we are not in movement, and if we are not using an object right now
        if (newTarget != null)
        {
            newTarget.SetOutline(true);
        }

        // Update cursor sprite (default if null)
        GameCursor.Instance.UpdateCursorSprite(newTarget);

        // Cache current
        _currentHoveringTarget = newTarget;
    }


    /// <summary>
    /// When we click on an PointClickTarget, we process it
    /// </summary>
    /// <param name="target"></param>
    public void HandleClick(PointClickTarget target, bool triggerRelease = false)
    {
        Debug.Log($"Player is interacting on a PointClickTarget! {target.gameObject.name}");
        //GameObject ob = target.gameObject;

        if (!triggerRelease)
        {
            switch (target.InteractionType)
            {
                case InteractionType.MoveTo:
                    // We had clicked on a point to go to ! 
                    Move(target);
                    return;
                case InteractionType.Object:
                    // We had clicked on a object to use
                    usedObject = target;
                    target.Use();
                    return;
                default:
                    Debug.Log("I'm gay");
                    return;
            }
        }
        else
        {
            Debug.Log($"Player is releasing interaction on a PointClickTarget! {target.gameObject.name}");
            target.Release();
            usedObject.SetOutline(true);
            usedObject = null;
        }

    }

    public void Move(PointClickTarget target)
    {
        isInMovement = true;

        // Convert the current Target before moving (if it's convertible)
        _currentHoveringTarget.Convert();
        // Convert the current anchor before moving (if it's convertible)
        currentAnchor?.Convert();

        // Get target transform from the clicked object
        Transform targetTransform = target.GetTransformTarget();

        GameCursor.Instance.Hide();
            
;        // Real 3D distance
        float moveDist = Vector3.Distance(cameraRig.position, targetTransform.position);

        // Angular distance in degrees
        float angleDist = Quaternion.Angle(cameraRig.rotation, targetTransform.rotation);

        // Convert to durations (speed => units/s or deg/s)
        float moveDuration = moveDist / moveSpeed;
        float rotateDuration = angleDist / rotateSpeed;

        // One shared duration so move + rotate end at the same time
        float duration = Mathf.Max(moveDuration, rotateDuration);

        cameraRig.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Join(cameraRig.DOMove(targetTransform.position, duration).SetEase(Ease.InOutSine));
        seq.Join(cameraRig.DORotateQuaternion(targetTransform.rotation, duration).SetEase(Ease.InOutSine));


        seq.OnComplete(() =>
        {
            // Move the cursor at 0 pos (Center the cursor in the screen)
            cursorPos = Vector3.zero;
            virtualCursor.anchoredPosition = Vector2.zero;

            //Show cursor
            GameCursor.Instance.Show();

            // Set yaw pitch to the right pos
            Vector3 euler = cameraRig.eulerAngles;
            yaw = euler.y;
            pitch = NormalizeAngle(euler.x);
            roll = NormalizeAngle(euler.z);

            // Update the current Anchor
            currentAnchor = target;

            // Un hide cursor
            isInMovement = false;
        });
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
            float t = Mathf.InverseLerp(1f, rampFactor, distMin);
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

        float speed = Mathf.InverseLerp(1f, rampFactor, distMin);

        yaw += cursorPos.x * lookSensitivity * speed * Time.deltaTime;
        pitch -= cursorPos.y * lookSensitivity * speed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion targetRot = Quaternion.Euler(pitch, yaw, roll);

        // Animation
        lookTween?.Kill();
        lookTween = cameraRig
            .DORotateQuaternion(targetRot, lookTweenDuration);
    }

    /// <summary>
    /// Return a boolean if the player can interact with this target
    /// </summary>
    /// <param name="target">Point and Click Target</param>
    /// <returns>Bool</returns>
    private bool CanInteract(PointClickTarget target)
    {
        if (target == null)
            return false;

        // If this target requires an anchor, player must be on it
        if (target.RequiredAnchor != null && target.RequiredAnchor != currentAnchor)
            return false;

        // If this is a MoveTo and we are already on it, block it
        if (target.InteractionType == InteractionType.MoveTo && target == currentAnchor)
            return false;

        return true;
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


    public float spring = 300f;
    public float damper = 10f;
    public float shake = 5f;
    public float dirIntensity = 0.1f;
    private void Shake()
    {

        Vector2 dir = Random.insideUnitCircle.normalized * dirIntensity;
        SDSShaker.Instance.Shake(spring, damper, shake, dir);
    }

}
