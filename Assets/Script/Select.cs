using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Select : MonoBehaviour
{
    public static GameObject Selected;

    public Color selectedColor;

    void OnMouseOver()
    {
        if (MainController._toggleSelect)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (gameObject.tag != "Ground" && Selected == null)
                {
                    Selected = this.gameObject;
                    Selected.GetComponent<Renderer>().material.color = selectedColor;
                }
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S)) MainController._toggleSelect = !MainController._toggleSelect;
        if (Selected != null)
        {
            bool moveRight = Input.GetKeyDown(KeyCode.RightArrow);
            if (moveRight) Selected.transform.position += Vector3.right * 0.2f;

            bool moveLeft = Input.GetKeyDown(KeyCode.LeftArrow);
            if (moveLeft) Selected.transform.position += Vector3.left * 0.2f;

            bool moveForward = Input.GetKeyDown(KeyCode.UpArrow);
            if (moveForward) Selected.transform.position += Vector3.forward * 0.2f;

            bool moveBackward = Input.GetKeyDown(KeyCode.DownArrow);
            if (moveBackward) Selected.transform.position += Vector3.back * 0.2f;

            bool moveUp = Input.GetKeyDown(KeyCode.PageUp);
            if (moveUp) Selected.transform.position += Vector3.up * 0.2f;

            bool moveDown = Input.GetKeyDown(KeyCode.PageDown);
            if (moveDown && Selected.transform.position.y > 0.2f) Selected.transform.position += Vector3.down * 0.2f;
        }

        if (Input.GetKeyDown(KeyCode.Delete) && Selected != null)
        {
            Destroy(Selected);
        }

    }

}
