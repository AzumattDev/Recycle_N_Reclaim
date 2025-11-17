namespace Recycle_N_Reclaim.GamePatches.UI;

public class ContainerRecyclingButtonHolder : MonoBehaviour
{
    private Button _recycleAllButton = null!;
    private bool _prefired;
    private TMP_Text _textComponent = null!;
    private Image _imageComponent = null!;

    public delegate void RecycleAllHandler();

    public event RecycleAllHandler OnRecycleAllTriggered = null!;

    private void Start()
    {
        InvokeRepeating(nameof(EnsureRecyclingButtonExistsIfPossible), 0f, 5f);
    }

    void EnsureRecyclingButtonExistsIfPossible()
    {
        if (InventoryGui.instance == null) return;
        if (_recycleAllButton == null)
        {
            SetupButton();
        }

        _recycleAllButton.gameObject.SetActive(ContainerRecyclingEnabled.Value.IsOn());
    }

    private void OnDestroy()
    {
        try
        {
            Destroy(_recycleAllButton.gameObject);
        }
        catch
        {
            // ignored
        }
    }

    private void FixedUpdate()
    {
        if (ContainerRecyclingEnabled.Value.IsOff()) return;
        if (_recycleAllButton == null) return;
        if (!InventoryGui.instance.IsContainerOpen() && _prefired) SetButtonState(false);
    }

    private void SetupButton()
    {
        if (_recycleAllButton != null)
            return;

        if (HasAuga)
        {
            _recycleAllButton = InventoryGui.instance.m_container.Find("RecycleAll").GetComponent<Button>();
        }
        else
        {
            var newLocalPosition = GetSavedButtonPosition();
            _recycleAllButton = Instantiate(InventoryGui.instance.m_takeAllButton, InventoryGui.instance.m_takeAllButton.transform);
            _recycleAllButton.transform.SetParent(InventoryGui.instance.m_takeAllButton.transform.parent);
            _recycleAllButton.transform.localPosition = newLocalPosition;
        }

        _recycleAllButton.onClick = new Button.ButtonClickedEvent();
        _recycleAllButton.onClick.AddListener(OnRecycleAllPressed);
        _textComponent = _recycleAllButton.GetComponentInChildren<TMP_Text>();
        _imageComponent = _recycleAllButton.GetComponentInChildren<Image>();
        var dragger = _recycleAllButton.gameObject.AddComponent<UIDragger>();
        dragger.OnUIDropped += (source, position) => { ContainerRecyclingButtonPositionJsonString.Value = position; };
        SetButtonState(false);
    }

    private Vector3 GetSavedButtonPosition()
    {
        var newLocalPosition = ContainerRecyclingButtonPositionJsonString.Value;
        return newLocalPosition;
    }

    private void SetButtonState(bool showPrefire)
    {
        if (showPrefire)
        {
            _prefired = true;
            _textComponent.text = Localize("$azumatt_recycle_n_reclaim_confirm");
            _imageComponent.color = new Color(1f, 0.5f, 0.5f);
        }
        else
        {
            _prefired = false;
            _textComponent.text = Localize("$azumatt_recycle_n_reclaim_reclaim_all");
            _imageComponent.color = new Color(0.5f, 1f, 0.5f);
        }
    }

    private void OnRecycleAllPressed()
    {
        if (!Player.m_localPlayer)
            return;
        if (!_prefired)
        {
            SetButtonState(true);
            return;
        }

        SetButtonState(false);
        OnRecycleAllTriggered?.Invoke();
    }
}