using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtmosphereResizer : MonoBehaviour
{

    public float lowSize;
    public float highSize;
    public Player player;

    private void Start()
    {
        player = player != null ? player : GetComponent<Player>();
        SetSize();
    }

    void Update()
    {
        SetSize();
    }

    void SetSize()
    {
        float size = Util.MapfClamped(player.currentElevation, player.minElevation, player.maxElevation, lowSize, highSize);
        if (size != transform.localScale.x)
            transform.localScale = new Vector3(size, size, size);
    }

}
