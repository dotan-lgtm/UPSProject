import express from "express";
import fetch from "node-fetch";
import cors from "cors";

const app = express();
app.use(cors()); // מאפשר גישה מהדפדפן

// קריאה ל-Commbox
app.get("/api/conversation/:id", async (req, res) => {
  const conversationId = req.params.id;

  const url = `https://api.commbox.io/streams/pE6q0uPbXiwBNLiTvKsDMw%3d%3d/objects/${conversationId}`;
  const options = {
    method: "GET",
    headers: {
      Authorization: "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6ImNtYi0zMi1kaWdpdC1rZXktaWQtMSJ9.eyJjbGllbnRfaWQiOiIzM2E1MjYyOWQwODg0ZTAxOWI4N2Q2M2UxNGJlY2FmMyIsImNsaWVudF9zZWNyZXQiOiI1Mzk5NGFiMmJlMzlhZjMwMjFjYjUwZDdlMDVmNTdjNGVlYzliMzdhMDMzMDAwYWMyNTZmOGZhNzRiMDcyZjcyIiwianRpIjoiMjFlMjRkYTUtY2IyOS00Njk5LTk1YWMtMzJhYmVjN2YwYzkwIiwibmJmIjoxNzYwNTEzMzQ4LCJleHAiOjE3ODgzNjgxOTQsImlhdCI6MTc2MDUxMzM0OH0.UAe8W28bOpq_4s4wA6DY7D4oReJ_dEFWZ76Qg312z20"
    }
  };

  try {
    const response = await fetch(url, options);
    const data = await response.json();
    res.json(data); // מחזיר את התשובה ל-Vue
  } catch (err) {
    console.error("Proxy error:", err);
    res.status(500).json({ error: err.message });
  }
});

app.listen(3000, () => console.log("Proxy running on http://localhost:3000"));
