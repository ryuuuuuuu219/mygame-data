using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤー／AI共通の入力集約クラス
/// Input.GetAxis() を一元管理して、他のスクリプトから値を取得する
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance;  // シングルトン的アクセス

    public InputActionAsset inputActions; // ← この中に「Player」マップが入っている
    private InputActionMap playerMap;

    [Header("Stick Axes")]
    public float horizontalL; // 左スティックX
    public float verticalL;   // 左スティックY
    public float horizontalR; // 右スティックX
    public float verticalR;   // 右スティックY
    public float accel;       // アクセル

    [Header("Triggers")]
    public float l2;
    public float r2;

    [Header("RButtons")]
    public bool submit;
    public bool cancel;

    public bool fireGun;
    public bool fireMissile;
    public bool changeWeapon;
    public bool targetChange;

    public bool north;
    public bool south;
    public bool west;
    public bool east;

    [Header("LButtons")]
    public bool up;
    public bool down;
    public bool left;
    public bool right;

    public bool stickL;
    public bool stickR;

    public bool l1;
    public bool altl2;
    public bool r1;
    public bool altr2;

    public bool menu;//10.15の目標

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        playerMap = inputActions.FindActionMap("Player");

        playerMap.Enable();
    }

    void Update()
    {
        // ---- スティック ----
        horizontalL = playerMap.FindAction("Roll").ReadValue<float>();
        verticalL = playerMap.FindAction("Pitch").ReadValue<float>();

        // 修正: ReadValue<Vector2>() の使用方法を修正
        Vector2 viewInput = playerMap.FindAction("View").ReadValue<Vector2>();
        horizontalR = viewInput.x;
        verticalR = viewInput.y;

        l1 = playerMap.FindAction("L1").IsPressed();
        r1 = playerMap.FindAction("R1").IsPressed();
        accel = (r1 ? 1f : 0f) + (l1 ? -1f : 0f);

        // ---- ボタン ----
        altl2 = playerMap.FindAction("L2").IsPressed();
        altr2 = playerMap.FindAction("R2").IsPressed();

        stickL = playerMap.FindAction("L3").IsPressed();
        stickR = playerMap.FindAction("R3").IsPressed();

        cancel = playerMap.FindAction("Cancel").WasPressedThisFrame();
        submit = playerMap.FindAction("Submit").WasPressedThisFrame();

        fireGun = playerMap.FindAction("Cancel").IsPressed();
        fireMissile = playerMap.FindAction("Submit").WasPressedThisFrame();

        changeWeapon = playerMap.FindAction("Square").WasPressedThisFrame();
        targetChange = playerMap.FindAction("Triangle").WasPressedThisFrame();
        menu = playerMap.FindAction("menu").WasPressedThisFrame();


        up = playerMap.FindAction("Up").IsPressed();
        down = playerMap.FindAction("Down").IsPressed();
        left = playerMap.FindAction("Left").IsPressed();
        right = playerMap.FindAction("Right").IsPressed();

        north = playerMap.FindAction("Triangle").IsPressed();
        south = playerMap.FindAction("Cancel").IsPressed();
        west = playerMap.FindAction("Square").IsPressed();
        east = playerMap.FindAction("Submit").IsPressed();

    }
}
