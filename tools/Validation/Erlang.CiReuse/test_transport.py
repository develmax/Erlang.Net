import unittest

from collector import storage_request


class TransportTests(unittest.TestCase):
    def test_storage_request_has_no_github_authorization(self):
        request = storage_request("https://example.blob.core.windows.net/artifact?sig=opaque")
        self.assertIsNone(request.get_header("Authorization"))
        self.assertEqual(request.get_header("Accept"), "application/octet-stream")

    def test_untrusted_or_downgraded_redirect_is_rejected(self):
        for address in ["http://example.blob.core.windows.net/x", "https://attacker.example/x",
                        "https://example.blob.core.windows.net.attacker.example/x",
                        "https://user:password@example.blob.core.windows.net/x"]:
            with self.subTest(address=address), self.assertRaises(ValueError):
                storage_request(address)


if __name__ == "__main__":
    unittest.main()
