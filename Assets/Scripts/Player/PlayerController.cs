using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 200f;
    public AudioSource shootSound;

    [SerializeField] private GameObject bulletPrefab;
    public Transform leftFirePoint; 
    public Transform rightFirePoint;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!IsOwner) return;

        HandleMovement();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            shootSound.Play();
            FireServerRpc();
        }
    }

    private void HandleMovement()
    {
        float moveInput = Input.GetAxis("Vertical");
        float rotateInput = Input.GetAxis("Horizontal");

        transform.Translate(Vector3.up * moveInput * moveSpeed * Time.deltaTime);
        transform.Rotate(Vector3.forward * -rotateInput * rotateSpeed * Time.deltaTime);
    }

    [ServerRpc]
    private void FireServerRpc()
    {
        GameObject leftBullet = Instantiate(bulletPrefab, leftFirePoint.position, leftFirePoint.rotation);
        leftBullet.GetComponent<NetworkObject>().Spawn();
        Rigidbody2D rbLeft = leftBullet.GetComponent<Rigidbody2D>();
        if (rbLeft != null)
        {
            rbLeft.linearVelocity = leftFirePoint.up * 10f; 
        }

        GameObject rightBullet = Instantiate(bulletPrefab, rightFirePoint.position, rightFirePoint.rotation);
        rightBullet.GetComponent<NetworkObject>().Spawn();

        Rigidbody2D rbRight = rightBullet.GetComponent<Rigidbody2D>();
        if (rbRight != null)
        {
            rbRight.linearVelocity = rightFirePoint.up * 10f; 
        }
    }
}