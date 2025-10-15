<template>
  <div class="app">
    <h1>Conversation Data</h1>

    <p v-if="conversationId">
      Conversation ID: <strong>{{ conversationId }}</strong>
    </p>

    <div v-if="custNo || trackNo" class="results">
    <p><strong>Customer No:</strong> {{ custNo }}</p>
    <p><strong>Track No:</strong> {{ trackNo }}</p>
    </div>

    <p v-else>
      No conversationId found in URL.
    </p>

    <div v-if="loading">Loading data...</div>

    <div v-else-if="error" class="error">
      Error: {{ error }}
    </div>

    <pre v-else class="response-box">
      {{ apiResponse }}
    </pre>
  </div>
</template>

<script>
export default {
  data() {
    return {
      conversationId: null,
      apiResponse: null,
      error: null,
      loading: false,
      custNo: null,
      trackNo: null
    };
  },
  methods: {
    getConversationIdFromUrl() {
      const params = new URLSearchParams(window.location.search);
      const id = params.get("conversationId");
      this.conversationId = id;
      console.log("Conversation ID:", id);
    },

    async fetchConversationData() {
    if (!this.conversationId) return;

    this.loading = true;
    this.error = null;

    const url = `http://localhost:3000/api/conversation/${this.conversationId}`;

    try {
        const res = await fetch(url);
        if (!res.ok) throw new Error(`HTTP error: ${res.status}`);
        const data = await res.json();

        // שמור את הפלט הגולמי
        this.apiResponse = JSON.stringify(data, null, 2);

        // 🔹 חילוץ user.content
        if (data?.data?.length > 0) {
        const userContentStr = data.data[0].user?.content;
        if (userContentStr) {
            const userContent = JSON.parse(userContentStr); // הפוך מחרוזת לאובייקט

            // שלוף cust_no ו-track_no
            this.custNo = userContent.cust_no || "N/A";
            this.trackNo = userContent.track_no || "N/A";
        }
        }
    } catch (err) {
        this.error = err.message;
        console.error("API error:", err);
    } finally {
        this.loading = false;
    }
    }



  },
  mounted() {
    this.getConversationIdFromUrl();
    this.fetchConversationData();
  }
};
</script>

<style>
.app {
  font-family: Calibri, sans-serif;
  text-align: center;
  margin: 50px auto;
  max-width: 800px;
}
.response-box {
  text-align: left;
  background: #f3f3f3;
  padding: 20px;
  border-radius: 8px;
  overflow-x: auto;
  white-space: pre-wrap;
}
.error {
  color: red;
  font-weight: bold;
}
</style>
