// Flushes Emscripten's in-memory file system (where Application.persistentDataPath lives on WebGL)
// to the browser's IndexedDB. Without it, a save written in this session can be lost when the page closes.
mergeInto(LibraryManager.library, {
  SyncFileSystemToIndexedDb: function () {
    FS.syncfs(false, function (err) {
      if (err) console.error("Save sync to IndexedDB failed: " + err);
    });
  }
});
