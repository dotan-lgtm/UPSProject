<template>
  <div class="app">
    <h1>תיוג קבצים מהשיחה</h1>

    <div v-if="loading" class="loading">טוען קבצים...</div>

    <div v-else-if="files.length > 0" class="file-list">
      <div v-for="(file, index) in files" :key="index" class="file-item">
        <div class="file-info">
          📎
          <a :href="file.fullPath" target="_blank" rel="noopener noreferrer">
            {{ file.name }}
          </a>
            <span v-if="file.uploadStatus" 
                :class="{'success': file.uploadStatus === '✅ הצלחה', 'fail': file.uploadStatus === '❌ כישלון'}"
                class="status">
            {{ file.uploadStatus }}
            </span>
        </div>

        <select v-model="file.selectedTag" class="tag-select">
          <option disabled value="">בחר קטגוריה...</option>
          <option v-for="tag in tags" :key="tag.code" :value="tag">
              {{ tag.name }}
          </option>
        </select>

      </div>
    </div>

    <div v-else class="no-files">
      לא נמצאו קבצים במייל זה.
    </div>

    <button class="submit-btn" @click="submitTags">סיום</button>
  </div>
</template>

<script>
export default {
  data() {
    return {
      conversationId: null,
      files: [],
      tags: [
        { name: "ללא תיוג", code: null },
        { name: "חשבונית ספק", code: 8 },
        { name: "הוכחות ייצוא", code: 5 },
        { name: "קטלוג", code: 4 },
        { name: "העברה בנקאית", code: 6 }
      ],
      loading: false,
      error: null,
      trackno: "",
      custno: ""
    };
  },
  methods: {
    // חילוץ conversationId מה-URL או שימוש ב-ID קבוע לבדיקה
    getConversationIdFromUrl() {
      const params = new URLSearchParams(window.location.search);
      const id = params.get("conversationId");
      this.conversationId = id || "5066549580797714"; // ← ID קבוע לבדיקה
      console.log("Conversation ID:", this.conversationId);
    },

    // טעינת קבצים מה-backend
    async fetchFiles() {
      if (!this.conversationId) return;

      this.loading = true;
      this.error = null;

      try {
        const res = await fetch(
          `http://localhost:5000/api/conversation/${this.conversationId}`
        );
        if (!res.ok) throw new Error(`HTTP error: ${res.status}`);

        const data = await res.json();

        // שליפת trackno ו-custno מתוך תגובת ה-backend
        this.trackno = data.meta?.trackno || "";
        this.custno = data.meta?.custno || "";
        console.log("TrackNo:", this.trackno, "CustNo:", this.custno);


        if (data.files && Array.isArray(data.files)) {
          // הוסף selectedTag ונתיב מלא לכל קובץ
          this.files = data.files.map(f => ({
            ...f,
            selectedTag: "",
            fullPath: f.path.startsWith("http")
              ? f.path
              : "https://upstest.commbox.io" + f.path
          }));
        }
      } catch (err) {
        console.error("API error:", err);
        this.error = err.message;
      } finally {
        this.loading = false;
      }
    },

    // הצגת גודל קובץ בפורמט קריא
    formatSize(bytes) {
      if (bytes < 1024) return bytes + " B";
      if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + " KB";
      return (bytes / (1024 * 1024)).toFixed(1) + " MB";
    },

    // שליחת התיוגים (כרגע רק הדפסה)
    async submitTags() {
        const payload = this.files
        .filter(f => f.selectedTag && f.selectedTag.code !== null) // 🆕 מדלג על "ללא תיוג"
        .map(f => ({
            name: f.name,
            path: f.fullPath,
            tagCode: f.selectedTag?.code,
            trackno: this.trackno,
            custno: this.custno
        }));


        try {
            const res = await fetch("http://localhost:5000/api/uploadFiles", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
            });

            if (!res.ok) throw new Error(`HTTP ${res.status}`);

            const results = await res.json();

            console.log("Data from backend", results);
            results.forEach(r => {
            console.log(
                `📎 קובץ: ${r.name} | קוד תיוג: ${r.doctypenum || "לא צוין"} | הצלחה: ${r.success} | הודעה: ${r.message}`
            );
            });


            this.files.forEach(f => {
            // דלג על קבצים עם תיוג "ללא תיוג" (code === null)
            if (f.selectedTag?.code === null) {
                f.uploadStatus = " קובץ זה לא תויג 🚫";
                return;
            }

            const match = results.find(r => r.name === f.name);
            f.uploadStatus = match?.success ? "✅ הצלחה" : "❌ כישלון";
            });

            alert("תהליך ההעלאה הסתיים");
        } catch (err) {
            console.error("שגיאה בשליחה:", err);
            alert("אירעה שגיאה בעת שליחת הקבצים");
        }
    }
  },
  mounted() {
    this.getConversationIdFromUrl();
    this.fetchFiles();
  }
};
</script>

<style scoped>
.app {
  font-family: Calibri, sans-serif;
  direction: rtl;
  text-align: center;
  margin: 50px auto;
  max-width: 700px;
}

.file-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
  margin-bottom: 30px;
}

.file-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #f5f5f5;
  padding: 12px 18px;
  border-radius: 8px;
  box-shadow: 0 0 5px rgba(0, 0, 0, 0.1);
}

.file-info a {
  color: #000;
  text-decoration: none;
  font-weight: bold;
}

.file-info a:hover {
  text-decoration: underline;
}

.file-size {
  color: #555;
  font-size: 0.9em;
  margin-right: 6px;
}

.tag-select {
  padding: 6px 8px;
  border-radius: 6px;
  border: 1px solid #ccc;
}

.submit-btn {
  background-color: #000;
  color: #fff;
  border: none;
  padding: 12px 24px;
  font-size: 16px;
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.3s;
}

.submit-btn:hover {
  background-color: #333;
}

.no-files {
  color: gray;
  margin-bottom: 20px;
}

.loading {
  font-weight: bold;
  color: #444;
  margin-bottom: 20px;
}


</style>
