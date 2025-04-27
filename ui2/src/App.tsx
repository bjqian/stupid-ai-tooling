// src/App.tsx
import { useState } from "react";

export default function App() {
  const [messages, setMessages] = useState<
    { from: "user" | "bot"; text: string }[]
  >([]);
  const [draft, setDraft] = useState("");

  const send = async () => {
    if (!draft.trim()) return;

    // optimistic UI
    setMessages([...messages, { from: "user", text: draft }]);
    setDraft("");

    const res = await fetch("http://localhost:8090/api/chat2", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        messages: [{ role: "user", content: [{ type: "text", text: draft }] }],
      }),
    });

    const data = await res.json();
    setMessages((m) => [...m, { from: "bot", text: data.text }]);
  };

  return (
    <main className="chat">
      <h1>My LLM Chat</h1>
      <div className="window">
        {messages.map((m, i) => (
          <p key={i} className={m.from}>
            {m.text}
          </p>
        ))}
      </div>

      <form
        onSubmit={(e) => {
          e.preventDefault();
          send();
        }}
      >
        <input
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          placeholder="Ask me anything…"
          autoFocus
        />
        <button>Send</button>
      </form>
    </main>
  );
}
