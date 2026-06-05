const jsonServer = require("json-server");
const multer = require("multer");
const { v4: uuidv4 } = require("uuid");
const path = require("path");
const fs = require("fs");
const express = require("express");

const app = jsonServer.create();
const router = jsonServer.router(path.join(__dirname, "db.json"));
const middlewares = jsonServer.defaults();
const uploadDir = path.join(__dirname, "uploads", "documents");
const PORT = process.env.PORT || 3000;
const HOST = process.env.HOST || "localhost";

fs.mkdirSync(uploadDir, { recursive: true });

const storage = multer.diskStorage({
  destination: (req, file, cb) => cb(null, uploadDir),
  filename: (req, file, cb) =>
    cb(null, `${uuidv4()}${path.extname(file.originalname)}`),
});

const upload = multer({ storage });

app.use(middlewares);
app.use("/uploads", express.static(path.join(__dirname, "uploads")));

app.post("/upload", upload.single("file"), (req, res) => {
  const fileName = req.file.filename;
  const host = req.headers.host;
  res.json({
    fileName,
    url: `http://${host}/uploads/documents/${fileName}`,
  });
});

app.delete("/upload/:filename", (req, res) => {
  const filePath = path.join(uploadDir, req.params.filename);
  fs.unlinkSync(filePath);
  res.json({ deleted: true });
});

app.use(router);
app.listen(PORT, "0.0.0.0", () => {});
