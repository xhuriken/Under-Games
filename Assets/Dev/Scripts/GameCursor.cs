using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cursor = UnityEngine.Cursor;

public class GameCursor : MonoBehaviour
{

    /*
     * This is an singleton class for the virtual cursor
     * It will contain all data relative for him like:
     * Cursor's sprites, Show/Hide function etc....
     */

    public static GameCursor Instance;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }

    [Header("Cursor")]
    [SerializeField] private Image virtualCursorImage;
    [SerializeField] private Sprite cursorDefault;
    [SerializeField] private Sprite cursorObject;
    [SerializeField] private Sprite cursorMoveTo;

    private Color cursorColor;
    private Color cursorAlpha;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cursorColor = virtualCursorImage.color;
        cursorAlpha = new Color(cursorColor.r, cursorColor.g, cursorColor.b, 0f);
    }

    /// <summary>
    /// Manage the cursor sprite, if we hover something and what, and if who hover nothing for reset
    /// </summary>
    /// <param name="target"></param>
    public void UpdateCursorSprite(PointClickTarget target)
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
    /// Hide the Game Cursor
    /// </summary>
    public void Hide()
    {
        // Clear previous tweens on this object to avoid conflicts
        this.DOKill();

        DOVirtual.Color(cursorColor, cursorAlpha, 0.10f, (value) =>
        {
            virtualCursorImage.color = value;
        }).SetTarget(this);
    }

    /// <summary>
    /// Show the Game Cursor
    /// </summary>
    public void Show()
    {
        // Clear previous tweens on this object to avoid conflicts
        this.DOKill();

        DOVirtual.Color(cursorAlpha, cursorColor, 1f, value =>
        {
            virtualCursorImage.color = value;
        }).SetTarget(this);

    }
}
