using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHarvester : MonoBehaviour
{
    public float rayDistance = 5f;
    public LayerMask hitMask = ~0;
    public int toolDamage = 1;
    public float hitCooldown = 0.15f;

    private float _nextHitTime;
    private Camera _cam;
    public Inventory inventory;
    InventoryUI invenUI;

    public GameObject selectedBlock;


    private void Awake()
    {
        _cam = Camera.main;
        if (inventory == null) inventory = gameObject.AddComponent<Inventory>();
        invenUI = FindObjectOfType<InventoryUI>();
    }

    void Update()
    {
        if (invenUI.selectedIndex < 0)
        {
            selectedBlock.transform.localScale = Vector3.zero;

            //선택된 idx가 -1이면 수확 모드
            if (Input.GetMouseButton(0) && Time.time >= _nextHitTime)
            {
                //Debug.Log("으엉");
                _nextHitTime = Time.time + hitCooldown;

                Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));  //화면 중앙
                if (Physics.Raycast(ray, out var hit, rayDistance, hitMask))
                {
                    var block = hit.collider.GetComponent<Block>();
                    if (block != null)
                    {
                        block.Hit(toolDamage, inventory);
                    }
                }
            }
        }
        else
        {
            Ray rayDebug = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); //화면 중앙
            if(Physics.Raycast(rayDebug, out var hitDebug, rayDistance, hitMask, QueryTriggerInteraction.Ignore))
            {
                Vector3Int placePos = AdjacentCellOnHitFace(hitDebug);
                selectedBlock.transform.localScale = Vector3.one;
                selectedBlock.transform.position = placePos;
                selectedBlock.transform.rotation = Quaternion.identity;
            }
            else
            {
                selectedBlock.transform.localScale = Vector3.zero;
            }

            if (Input.GetMouseButtonDown(0))
            {
                //선택된 idx가 0 이상이면 설치 모드
                Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));  //화면 중앙
                if (Physics.Raycast(ray, out var hit, rayDistance, hitMask, QueryTriggerInteraction.Ignore))
                {
                    Vector3Int placePos = AdjacentCellOnHitFace(hit);

                    BlockType selected = invenUI.GetInventorySlot();
                    if (inventory.Consume(selected, 1))
                    {
                        FindObjectOfType<NoiseVoxelMap>().PlaceTile(placePos, selected);
                    }
                }
            }
        }

    }

    static Vector3Int AdjacentCellOnHitFace(in RaycastHit hit)
    {
        Vector3 baseCenter = hit.collider.transform.position;   //맞춘 블록의 중심
        Vector3 adjCenter = baseCenter + hit.normal;
        return Vector3Int.RoundToInt(adjCenter);
    }
}