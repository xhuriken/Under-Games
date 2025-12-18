using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    public float temp = 3f;

    [Header("Look")]
    [Tooltip("Mouse sensitivity for camera rotation.")]
    [SerializeField] private float lookSensitivity = 2.5f;


    [Tooltip("Clamp for vertical rotation (pitch).")]
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 80f;

    [Tooltip("DOTween smoothing duration for look rotation.")]
    [SerializeField] private float lookTweenDuration = 0.08f;

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

        
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {

            //Launch raycast from mouse position

            //Get Mouse Position
            Vector3 mousePos = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            RaycastHit hit;
            
            //launch raycast
            if (Physics.Raycast(ray, out hit))
            {
                //Check if the object hit has a PointClickTarget component
                PointClickTarget target = hit.collider.GetComponent<PointClickTarget>();

                if (target != null) HandleClick(target);
            }
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


                return;
            default:
                Debug.Log("I'm gay");
                return;
        }
    }

    public void Move(PointClickTarget target)
    {
        if (isInMovement) return;
        isInMovement = true;

        Transform targetTransform = target.GetTransformTarget();

        Transform cam = _camera.transform;

        cam.DOKill();

        Sequence seq = DOTween.Sequence();
        
        //TODO: Polish it !
        seq.Join(cam.DOMove(targetTransform.position, 0.5f));

        seq.Join(cam.DORotateQuaternion(targetTransform.rotation, 0.5f));

        seq.OnComplete(() =>
        {
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


    float EllipseDistance(Vector2 p, Vector2 ellipse)
    {
        return Mathf.Sqrt(
            (p.x * p.x) / (ellipse.x * ellipse.x) +
            (p.y * p.y) / (ellipse.y * ellipse.y)
        );
    }

    Vector2 ClampToEllipse(Vector2 p, Vector2 ellipse)
    {
        float d = EllipseDistance(p, ellipse);
        if (d <= 1f) return p;
        return p / d;
    }


}
